using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
public enum IrishPotLuckThrowType
{
    Wild, Scatter
}

[System.Serializable]
public class IrishPotLuckThrowTarget
{
    public IrishPotLuckSlotScript slot;
    public Vector3 targetPosition;
    public IrishPotLuckThrowType throwType;

    public IrishPotLuckThrowTarget(IrishPotLuckSlotScript slot, Vector3 targetPosition, IrishPotLuckThrowType throwType)
    {
        this.slot = slot;
        this.targetPosition = targetPosition;
        this.throwType = throwType;
    }
}
public class IrishPotLuckCharacterController : MonoBehaviour
{
    #region Variables 

    [Header("Character")]
    [SerializeField] private Animator characterAnimator;

    [Header("Wild Throw")]
    [SerializeField] private Transform movingSpawnParent;
    [SerializeField] private GameObject movingWildObject;
    [SerializeField] private GameObject movingScatterObject;
    [SerializeField] private GameObject instantiatedSlotPrefab;

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 0.9f;
    [SerializeField] private Ease moveEase = Ease.InBack;
    public float jumpPower = 2.5f;

    private const string Normal = "Normal";
    private const string Spin = "Spin";
    private const string Win = "Win";
    private const string Throw = "Throw";
    private const string ThrowCoin = "ThrowCoin";

    [Header("Instantiated Slot Child Index")]
    [SerializeField] private int wildChildIndex = 8;
    [SerializeField] private int scatterChildIndex = 9;

    [Header("Spawned Slot Animation Bools")]
    [SerializeField] private string spawnedWildSlotBoolName = "WildFlip";
    [SerializeField] private string spawnedScatterSlotBoolName = "ScatterFlip";
    [Header("Spawned Slot Payline Animation Bools")]
    [SerializeField] private string spawnedWildPaylineBoolName = "Wild";
    [SerializeField] private string spawnedScatterPaylineBoolName = "Scatter";

    bool canSpawnCoin = false;
    bool canDetachCoin = false;

    void SetCanSpawnCoin() => canSpawnCoin = true;
    void SetCanDetachCoin() => canDetachCoin = true;

    private Coroutine returnCoroutine;
    private Coroutine throwCoroutine;

    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    public List<IrishPotLuckSpawnedSlotInstance> activeSpawnedSlots = new List<IrishPotLuckSpawnedSlotInstance>();
    public bool IsThrowPlaying { get; private set; }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        SetMovingObjectsActive(false);
    }
    #endregion

    #region Public Refernces
    public void PlayNormal()
    {
        SetCharacterState(Normal);
    }

    public void PlaySpinThenNormal(float delay = 2f)
    {
        SetCharacterState(Spin);
        returnCoroutine = StartCoroutine(ReturnToNormal(delay));
    }

    public void PlayWinThenNormal(float delay = 2f)
    {
        SetCharacterState(Win);
        returnCoroutine = StartCoroutine(ReturnToNormal(delay));
    }

    public void PlayThrow()
    {
        SetCharacterState(ThrowCoin);
    }

    private void SetCharacterState(string activeState)
    {
        StopReturnCoroutine();

        if (characterAnimator == null)
            return;

        characterAnimator.SetBool(Normal, activeState == Normal);
        characterAnimator.SetBool(Spin, activeState == Spin);
        characterAnimator.SetBool(Win, activeState == Win);
        characterAnimator.SetBool(Throw, activeState == Throw);

        if (activeState == ThrowCoin)
            characterAnimator.SetTrigger(ThrowCoin);
    }

    private IEnumerator ReturnToNormal(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayNormal();
    }

    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null)
            return;

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }
    #endregion

    #region Wild Throw

    public void PlayThrowTargets(List<IrishPotLuckThrowTarget> targets)
    {
        if (throwCoroutine != null)
            StopCoroutine(throwCoroutine);

        throwCoroutine = StartCoroutine(ThrowTargetsRoutine(targets));
    }
    private IEnumerator ThrowTargetsRoutine(List<IrishPotLuckThrowTarget> targets)
    {
        IsThrowPlaying = true;

        if (targets == null || targets.Count == 0)
        {
            IsThrowPlaying = false;
            throwCoroutine = null;
            yield break;
        }

        foreach (IrishPotLuckThrowTarget target in targets)
        {
            if (target == null || target.slot == null)
                continue;

            yield return StartCoroutine(ThrowSingleTarget(target));
        }

        PlayNormal();

        IsThrowPlaying = false;
        throwCoroutine = null;
    }
    private IEnumerator ThrowSingleTarget(IrishPotLuckThrowTarget target)
    {
        GameObject movingObject = GetMovingObject(target.throwType);

        if (movingObject == null || movingSpawnParent == null)
            yield break;

        canSpawnCoin = false;
        canDetachCoin = false;

        PlayThrow();

        yield return new WaitUntil(() => canSpawnCoin);
        canSpawnCoin = false;

        GameObject movingInstance = Instantiate(movingObject, movingSpawnParent.position, movingSpawnParent.rotation, movingSpawnParent);

        movingInstance.SetActive(true);

        yield return new WaitUntil(() => canDetachCoin);
        canDetachCoin = false;

        movingInstance.transform.SetParent(null, true);

        yield return movingInstance.transform
            .DOJump(target.targetPosition, jumpPower, 1, moveDuration)
            .SetSpeedBased(true)
            .SetEase(moveEase)
            .WaitForCompletion();

        SpawnInstantiatedSlot(target);

        movingInstance.SetActive(false);
        Destroy(movingInstance);
    }
    private GameObject GetMovingObject(IrishPotLuckThrowType throwType)
    {
        return throwType == IrishPotLuckThrowType.Wild ? movingWildObject : movingScatterObject;
    }

    private void SpawnInstantiatedSlot(IrishPotLuckThrowTarget target)
    {
        if (instantiatedSlotPrefab == null || target == null || target.slot == null)
            return;

        GameObject spawnedSlot = Instantiate(instantiatedSlotPrefab);

        spawnedSlot.transform.SetParent(target.slot.transform.parent);
        spawnedSlot.transform.position = target.targetPosition;
        spawnedSlot.transform.SetParent(null);

        SetOnlyTargetChildActive(spawnedSlot, target.throwType);

        Animator spawnedAnimator = spawnedSlot.GetComponent<Animator>();
        string flipBool = GetSpawnedSlotFlipBoolName(target.throwType);
        string paylineBool = GetSpawnedSlotPaylineBoolName(target.throwType);

        if (spawnedAnimator != null)
        {
            spawnedAnimator.enabled = true;
            StartCoroutine(PlaySpawnBoolOnce(spawnedAnimator, flipBool));
        }

        activeSpawnedSlots.Add(new IrishPotLuckSpawnedSlotInstance
        {
            originalSlot = target.slot,
            instance = spawnedSlot,
            animator = spawnedAnimator,
            flipBool = flipBool,
            paylineBool = paylineBool,
            reelIndex = target.slot.reelIndex,
            slotIndex = target.slot.slotIndex
        });

        spawnedSlots.Add(spawnedSlot);
    }
    private string GetSpawnedSlotFlipBoolName(IrishPotLuckThrowType throwType)
    {
        return throwType == IrishPotLuckThrowType.Wild ? spawnedWildSlotBoolName : spawnedScatterSlotBoolName;
    }

    private string GetSpawnedSlotPaylineBoolName(IrishPotLuckThrowType throwType)
    {
        return throwType == IrishPotLuckThrowType.Wild ? spawnedWildPaylineBoolName : spawnedScatterPaylineBoolName;
    }
    private void SetOnlyTargetChildActive(GameObject spawnedSlot, IrishPotLuckThrowType throwType)
    {
        if (spawnedSlot == null)
            return;

        SetChildActive(spawnedSlot, wildChildIndex, throwType == IrishPotLuckThrowType.Wild);
        SetChildActive(spawnedSlot, scatterChildIndex, throwType == IrishPotLuckThrowType.Scatter);
    }

    private void SetChildActive(GameObject parent, int index, bool state)
    {
        if (parent == null)
            return;

        if (index < 0 || index >= parent.transform.childCount)
            return;

        parent.transform.GetChild(index).gameObject.SetActive(state);
    }
    private IEnumerator PlaySpawnBoolOnce(Animator animator, string boolName)
    {
        if (animator == null || string.IsNullOrEmpty(boolName))
            yield break;

        animator.SetBool(boolName, true);

        yield return new WaitForSeconds(1f);

        if (animator != null)
            animator.SetBool(boolName, false);
    }
    public bool SetSpawnedSlotPaylineAnimation(IrishPotLuckSlotScript slot, bool play)
    {
        if (slot == null)
            return false;

        IrishPotLuckSpawnedSlotInstance spawned = activeSpawnedSlots.Find(x =>
            x.reelIndex == slot.reelIndex &&
            x.slotIndex == slot.slotIndex
        );

        if (spawned == null || spawned.animator == null || string.IsNullOrEmpty(spawned.paylineBool))
            return false;

        spawned.animator.enabled = true;
        spawned.animator.SetBool(spawned.paylineBool, play);

        return true;
    }
    public void ClearSpawnedSlots()
    {
        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }

        IsThrowPlaying = false;
        canSpawnCoin = false;
        canDetachCoin = false;

        for (int i = activeSpawnedSlots.Count - 1; i >= 0; i--)
        {
            if (activeSpawnedSlots[i] != null && activeSpawnedSlots[i].instance != null)
                Destroy(activeSpawnedSlots[i].instance);
        }

        activeSpawnedSlots.Clear();
        spawnedSlots.Clear();
        SetMovingObjectsActive(false);
    }

    private void SetMovingObjectsActive(bool state)
    {
        if (movingWildObject != null)
            movingWildObject.SetActive(state);

        if (movingScatterObject != null)
            movingScatterObject.SetActive(state);
    }
    #endregion
}

[System.Serializable]
public class IrishPotLuckSpawnedSlotInstance
{
    public IrishPotLuckSlotScript originalSlot;
    public GameObject instance;
    public Animator animator;

    public string flipBool;
    public string paylineBool;

    public int reelIndex;
    public int slotIndex;
}