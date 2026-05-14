using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StickyPiggyFreeSpinLocker : MonoBehaviour
{
    #region Variables

    public static StickyPiggyFreeSpinLocker Instance;
    [Header("References")]
    [SerializeField] private Transform freeSpinUIParent;
    [SerializeField] private StickyPiggyLocker lockerPrefab;
    private StickyPiggyLocker locker;
   
    public GameObject spin1Prefab;
    public GameObject spin2Prefab;
    public GameObject spin3Prefab;
    public GameObject keyBox;
    public List<GameObject> keys = new List<GameObject>();

    [SerializeField] private TMP_Text totalSpinText;
    private int currentDisplayedSpins = 0;

    public int totalSpins;
    public List<int> triggerReels = new List<int>();

    public GameObject logoImage;
    public GameObject freeSpinsTextObject;
    private TMP_Text freeSpinsText;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinsText = freeSpinsTextObject.transform.GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References
    [ContextMenu("Start Animations")]
    public void StartLockerTransition()
    {
        if(locker != null)
        {
            Destroy(locker.gameObject);
        }
        locker = Instantiate(lockerPrefab, freeSpinUIParent);
        currentDisplayedSpins = 0;
        UpdateSpinText();
        ResetAllBoxes();
        ResetChains();
        StartCoroutine(StartAnimations());
    }

    private IEnumerator StartAnimations()
    {
        yield return new WaitUntil(() => StickyPiggyUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => StickyPiggySlotMachine.Instance.isSlotAnimationCompleted);
        ShowFreeSpinsText();
        StickyPiggyUIManager.Instance.StopMusic("BG");
        StickyPiggyUIManager.Instance.PlayMusic("FreeSpinBg");
        yield return StartCoroutine(StickyPiggyPaylineController.Instance.BonusCollect());

        StickyPiggyPaylineController.Instance.StopPaylines();
        StickyPiggyPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(0.5f);
        PopupAnimation(locker.lockerObject, 1f, 0.5f, true);

        yield return new WaitForSeconds(2.5f);

        StickyPiggyPaylineController.Instance.ResetAllSlotsToDefault();
        yield return StartCoroutine(OpenBoxesFromBonusReels());
    }
    private IEnumerator EndAnimations()
    {
        yield return new WaitForSeconds(0.5f);
        keyBox.SetActive(false);
        Transform parent = keyBox.transform.GetChild(0);
        parent.GetChild(0).gameObject.SetActive(true);
        parent.GetChild(1).gameObject.SetActive(false);
        PopupAnimation(locker.lockerObject, 0f, 0.5f, false);
        StickyPiggyFreeGameTransitionController.Instance.StartFreeSpinTransition();
    }

    private void UpdateSpinText()
    {
        totalSpinText.text = currentDisplayedSpins.ToString();
    }
    #endregion

    #region Locker Logic
    private IEnumerator OpenBoxesFromBonusReels()
    {
        triggerReels = StickyPiggySlotMachine.Instance.freeSpinTriggerReelIndexes;

        if (triggerReels == null || triggerReels.Count == 0)
        {
            Debug.LogWarning("No free spin trigger reel indexes found.");
            yield break;
        }
        triggerReels.Sort();

        yield return StartCoroutine(AnimateTriggeredChains(triggerReels));
        HashSet<int> boxIndexesToOpen = new HashSet<int>();

        foreach (int reelIndex in triggerReels)
        {
            List<int> mappedBoxes = GetBoxIndexesFromReelIndex(reelIndex);

            foreach (int boxIndex in mappedBoxes)
            {
                boxIndexesToOpen.Add(boxIndex);
            }
        }

        Transform parent = keyBox.transform.GetChild(0);
        parent.GetChild(0).gameObject.SetActive(false);
        parent.GetChild(1).gameObject.SetActive(true);
        ResetKeys();
        var spinData = CalculateSpinDistribution(boxIndexesToOpen);
        yield return StartCoroutine(OpenSelectedBoxesWithSpins(spinData));
    }

    private List<int> GetBoxIndexesFromReelIndex(int reelIndex)
    {
        List<int> result = new List<int>();

        if (reelIndex < 0 || reelIndex > 4)
        {
            Debug.LogWarning("Invalid reel index for locker mapping: " + reelIndex);
            return result;
        }

        result.Add(reelIndex);
        result.Add(reelIndex + 5);
        result.Add(reelIndex + 10);

        return result;
    }

    private IEnumerator AnimateTriggeredChains(List<int> reelIndexes)
    {
        for (int i = 0; i < reelIndexes.Count; i++)
        {
            int reelIndex = reelIndexes[i];

            if (reelIndex < 0 || reelIndex >= locker.chainObjects.Count)
                continue;

            GameObject chain = locker.chainObjects[reelIndex];
            if (chain == null)
                continue;

            chain.SetActive(true);

            Animator chainAnimator = chain.GetComponent<Animator>();
            if (chainAnimator != null)
            {
                chainAnimator.enabled = true;
                chainAnimator.SetBool("Play", true);
            }

            yield return new WaitForSeconds(locker.chainAnimationDuration);
        }

        yield return new WaitForSeconds(0.5f);
    }
    #endregion

    #region Spin Distribution

    private Dictionary<int, int> CalculateSpinDistribution(HashSet<int> boxIndexes)
    {
        Dictionary<int, int> spinPerBox = new Dictionary<int, int>();

        totalSpins = StickyPiggySlotMachine.Instance.freeSpinCount;
        int boxCount = boxIndexes.Count;

        if (boxCount <= 0)
        {
            Debug.LogWarning("Cannot calculate spin distribution: no boxes available.");
            return spinPerBox;
        }

        //assign 1 spin to all
        foreach (int box in boxIndexes)
        {
            spinPerBox[box] = 1;
        }

        int remainingSpins = totalSpins - boxCount;

        if (remainingSpins <= 0)
            return spinPerBox;

        List<int> boxList = new List<int>(boxIndexes);

        //random feel
        Shuffle(boxList);

        int i = 0;

        while (remainingSpins > 0)
        {
            int box = boxList[i];
            spinPerBox[box]++;
            remainingSpins--;

            i++;
            if (i >= boxList.Count)
                i = 0;
        }

        return spinPerBox;
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    #endregion

    #region Box Opening
    private IEnumerator OpenSelectedBoxesWithSpins(Dictionary<int, int> spinData)
    {
        List<int> sortedKeys = new List<int>(spinData.Keys);
        sortedKeys.Sort();

        foreach (int boxIndex in sortedKeys)
        {
            int spinCount = spinData[boxIndex];

            if (boxIndex < 0 || boxIndex >= locker.lockBoxes.Count)
                continue;

            GameObject box = locker.lockBoxes[boxIndex];

            yield return StartCoroutine(OpenSingleBox(box, spinCount));
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(EndAnimations());
    }
    private IEnumerator OpenSingleBox(GameObject box, int spinCount)
    {
        box.SetActive(true);

        Animator boxAnimator = box.GetComponent<Animator>();
        if (boxAnimator != null)
        {
            boxAnimator.enabled = true;
            boxAnimator.SetBool("Open", true);
        }
        yield return new WaitForSeconds(0.2f);

        yield return new WaitForSeconds(0.3f);
        
        SetSpinVisual(box, spinCount);
        currentDisplayedSpins += spinCount;
        UpdateSpinText();
    }
    private void SetSpinVisual(GameObject box, int spinCount)
    {
        Transform container = box.transform.GetChild(1);

        if (container == null)
        {
            Debug.LogWarning("Container not found in box");
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        GameObject prefabToUse = null;

        switch (spinCount)
        {
            case 1: prefabToUse = spin1Prefab; break;
            case 2: prefabToUse = spin2Prefab; break;
            case 3: prefabToUse = spin3Prefab; break;
            default:
                prefabToUse = spin3Prefab;
                break;
        }
        GameObject instance = Instantiate(prefabToUse, container);
        instance.SetActive(true);

        //Reset transform
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one;
        instance.transform.localRotation = Quaternion.identity;
        Animator spinAnimator = instance.GetComponent<Animator>();
        if (spinAnimator != null)
        {
            spinAnimator.enabled = true;
            spinAnimator.SetBool("Play", true);  
        }

    }

    #endregion

    #region Helper Functions
    public void ShowTriggeredKeys()
    {
        List<int> triggerReels = StickyPiggySlotMachine.Instance.freeSpinTriggerReelIndexes;

        for (int i = 0; i < keys.Count; i++)
        {
            if (triggerReels.Contains(i))
            {
                keys[i].transform.GetChild(1).gameObject.SetActive(true);
            }
        }
    }
    public void ShowFreeSpinsText()
    {
        int triggerCount = StickyPiggySlotMachine.Instance.freeSpinTriggerReelIndexes.Count;
        int freeSpins = 0;

        switch (triggerCount)
        {
            case 3: freeSpins = 9; break;
            case 4: freeSpins = 12; break;
            case 5: freeSpins = 15; break;
            default: break;
        }
        freeSpinsText.text = "Win " + freeSpins + "+ Free Spins!";
        logoImage.SetActive(false);
        freeSpinsTextObject.SetActive(true);
    }
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(true);

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.gameObject.AddComponent<CanvasGroup>();

        obj.transform.DOKill();
        cg.DOKill();

        if (state)
        {
            obj.transform.localScale = Vector3.one * 0.8f;
            cg.alpha = 0f;

            obj.transform.DOScale(scale, duration).SetEase(Ease.OutQuad);
            cg.DOFade(1f, duration);
        }
        else
        {
            obj.transform.DOScale(0.9f, duration).SetEase(Ease.InQuad);
            cg.DOFade(0f, duration).OnComplete(() =>
            {
                obj.transform.parent.gameObject.SetActive(false);
            });
        }
    }

    #endregion

    #region Reset Methods
    public void ResetKeys()
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null)
                keys[i].transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    private void ResetAllBoxes()
    {
        currentDisplayedSpins = 0;
        UpdateSpinText();
        foreach (var box in locker.lockBoxes)
        {
            if (box == null) continue;

            box.SetActive(true);

            Animator boxAnimator = box.GetComponent<Animator>();
            if (boxAnimator != null)
            {
                boxAnimator.enabled = true;
                boxAnimator.SetBool("Open", false);
            }

            Transform container = box.transform.GetChild(1);
            if (container != null)
            {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Destroy(container.GetChild(i).gameObject);
                }
            }

            Transform closedVisual = box.transform.GetChild(2);
            if (closedVisual != null)
            {
                closedVisual.gameObject.SetActive(true);
            }

            box.transform.localScale = Vector3.one;
        }
    }
    public void ResetFreeSpinHeader()
    {
        logoImage.SetActive(true);
        freeSpinsTextObject.SetActive(false);

        if (freeSpinsText != null)
            freeSpinsText.text = "";
    }
    private void ResetChains()
    {
        foreach (var chain in locker.chainObjects)
        {
            if (chain == null) continue;

            chain.SetActive(true);
            chain.transform.localScale = Vector3.one;

            Animator chainAnimator = chain.GetComponent<Animator>();
            if (chainAnimator != null)
            {
                chainAnimator.enabled = true;
                chainAnimator.SetBool("Play", false);
            }
        }
    }
    #endregion
}