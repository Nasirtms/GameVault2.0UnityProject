using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DayOfDeadFreeSpinController : MonoBehaviour
{
    #region Variables

    public static DayOfDeadFreeSpinController Instance;


    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1f;
    private bool isFreeGame = false;
    private bool firstSpin;

    [Header("Topbar Settings")]
    [SerializeField] public DayOfDeadFreeSpinTopBar topbar;   // <- new topbar script
    [SerializeField] private int initialTopbarTokens = 2;

    [Header("Walking Wild Settings")]
    [SerializeField] private GameObject walkingWildPrefab;

    public int currentMultiplier = 2;

    // only walking wilds live here now

    public int reelIndex = 0;
    public int slotIndex = 0;
    public GameObject wildParticles;
    #endregion

    #region Public References
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        isFreeGame = true;
        firstSpin = true;
        DayOfDeadSlotMachine.Instance.ClearExpandingWilds();
        DayOfDeadSlotMachine.Instance.isRespinActive = false;
        DayOfDeadSlotMachine.Instance.isReSpin = false;

        //topbar.topbarArea.SetActive(true);
        if (topbar != null)
            topbar.CreateInitialTokens(initialTopbarTokens);

        currentMultiplier = 2;
        UpdateSpinText();

        StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        ClearActiveWilds();

        if (topbar != null)
            topbar.ClearAllTokens();

        currentMultiplier = 1;
        UpdateSpinText();

        isFreeGame = false;
    }

    public void InitialFreeSpinText()
    {
        currentMultiplier = 2;
        UpdateSpinText();
    }
    public void ErrorFreeSpinReturn()
    {
        UpdateSpinText();
    }
    private void UpdateSpinText()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{currentMultiplier}X";
    }
    #endregion

    #region Walking Wild Handling

    private void ClearActiveWilds()
    {
        foreach (var w in DayOfDeadSlotMachine.Instance.activeWilds)
        {
            if (w.instance != null) Destroy(w.instance);
        }
        DayOfDeadSlotMachine.Instance.activeWilds.Clear();
    }

    private void MoveWildsLeft()
    {
        if (DayOfDeadSlotMachine.Instance.activeWilds.Count == 0) return;

        List<FreeSpinWalkingWild> toRemove = new List<FreeSpinWalkingWild>();

        foreach (var wild in DayOfDeadSlotMachine.Instance.activeWilds)
        {
            wild.reelIndex--;

            if (wild.reelIndex < 0)
            {
                if (!HasFreeSpin())
                {
                    DG.Tweening.DOVirtual.DelayedCall(1.5f, () =>
                    {
                        if (wild.instance != null)
                            Destroy(wild.instance);
                    });
                }
                else
                {
                    if (wild.instance != null)
                        Destroy(wild.instance);
                }

                toRemove.Add(wild);

                currentMultiplier++;
            }
        }

        foreach (var w in toRemove)
        {
            DayOfDeadSlotMachine.Instance.activeWilds.Remove(w);
        }
    }
    public GameObject lastToken;
    public GameObject newSlot;
    public IEnumerator MoveWildFromTopbarForSpin()
    {
        if (topbar == null || topbar.tokens.Count == 0)
            yield break;

        if (DayOfDeadSlotMachine.Instance.activeWilds.Count > 0)
            yield break;


        DayOfDeadSlotScript slot = DayOfDeadSlotMachine.Instance.GetRandomWalkingWildSlot();
        if (slot == null)
            yield break;

        lastToken = topbar.GetToken();

        int reelIndex = slot.reelIndex;
        int slotIndex = slot.slotIndex;

        Sequence wildSeq = DOTween.Sequence();

        topbar.MoveParticles(lastToken, slot.transform.position, () =>
        {
            if (topbar != null)
                topbar.RemoveLastToken();

            wildSeq.Play();
        });

        newSlot = Instantiate(walkingWildPrefab);
        GameObject newSlotPrefab = newSlot.transform.GetChild(11).gameObject;

        DayOfDeadAnimationController animCtrl = newSlot.GetComponent<DayOfDeadAnimationController>();
        FreeSpinWalkingWild instance = new FreeSpinWalkingWild
        {
            slot = slot,
            instance = newSlot,
            reelIndex = reelIndex,
            slotIndex = slotIndex,
            animController = animCtrl
        };


        wildSeq.AppendInterval(0.5f).AppendCallback(() =>
               {
                   newSlotPrefab.SetActive(true);
                   newSlot.transform.parent = slot.transform.parent;
                   newSlot.transform.position = slot.transform.position;
                   newSlot.transform.localScale = slot.transform.localScale;
                   newSlot.transform.parent = null;
               })
               .AppendInterval(0.5f).AppendCallback(() =>
               {
                   instance.animController.PlaySmallToBigOnce();
               })

               .AppendCallback(() =>
               {
                   DayOfDeadSlotMachine.Instance.MoveFreeSpinWildToRow3(instance);
               })

               .AppendInterval(1.5f)

               .AppendCallback(() =>
               {
                   var child11 = newSlot.transform.GetChild(11).gameObject;
                   var child14 = newSlot.transform.GetChild(14).gameObject;
                   child11.SetActive(false);
                   child14.SetActive(true);
               });

        DayOfDeadSlotMachine.Instance.activeWilds.Add(instance);
    }

    #endregion

    #region Free Spin
    public bool isWildParticle = true;
    private bool HasFreeSpin()
    {
        int tokenCount = (topbar != null) ? topbar.tokens.Count : 0;
        return tokenCount > 0 || DayOfDeadSlotMachine.Instance.activeWilds.Count > 0;
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(1f); 

        while (HasFreeSpin())
        {
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins); 
            }

            MoveWildsLeft();

            if (!HasFreeSpin())
                break;

            DayOfDeadSlotMachine.Instance.currentFreeSpinMultiplier = currentMultiplier;
            UpdateSpinText();

            yield return new WaitUntil(() => isWildParticle);

            float betAmount = DayOfDeadUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isSpinAgain);

            if (DayOfDeadSlotMachine.Instance.currentSpinResult != null && DayOfDeadSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(0.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        DayOfDeadSlotMachine.Instance.isFreeGame = false;
        topbar.topbarArea.SetActive(false);
        DayOfDeadFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    public void HandleFreeSpinWild(DayOfDeadSlotScript slot)
    {
        if (slot == null || topbar == null) return;
        StartCoroutine(HandleFreeSpinWildLanded(slot));
    }
    private IEnumerator HandleFreeSpinWildLanded(DayOfDeadSlotScript slot)
    {
        yield return new WaitForSeconds(0.5f);
        isWildParticle = false;

        slot.PlayAnimation();
        yield return new WaitForSeconds(0.7f);
        slot.StopAnimation();

        topbar.AddToken();  
        GameObject lastToken = topbar.GetToken();

        if (lastToken != null)
        {
            Vector3 tokenWorldPos = lastToken.transform.position;

            yield return slot.MoveParticles(tokenWorldPos);
        }
        isWildParticle = true;
    }
    #endregion
}

[System.Serializable]
public class FreeSpinWalkingWild
{
    public DayOfDeadSlotScript slot;
    public GameObject instance;
    public int reelIndex;
    public int slotIndex;
    public DayOfDeadAnimationController animController;
}
[System.Serializable]
public class FreeSpinLockedSlot
{
    public int reelIndex;
    public int slotIndex;
}