using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CashVaultBlindFeature : MonoBehaviour
{
    #region Variables

    public static CashVaultBlindFeature Instance;

    [Header("Vault Buttons")]
    [SerializeField] private Button leftVaultButton;
    [SerializeField] private Button rightVaultButton;

    [Header("Reward Prefabs / Objects")]
    [SerializeField] private GameObject freeSpinFeature;
    [SerializeField] private GameObject HoldnLinkFeature;
    public bool showFreeSpin;
    public bool showHoldnLink;
    public bool vaultAlreadyClicked = false;

    public bool isBlindFeatureCompleted;
    public System.Action OnBlindFeatureCompleted;

    [Header("FreeSpin Reel")]
    [SerializeField] private GameObject blindFeatureStartFrame;
    [SerializeField] private GameObject freeSpinPopup;
    [SerializeField] private RectTransform freeSpinReelBox;
    [SerializeField] private RectTransform wildReelBox;

    [SerializeField] private float slotHeight = 45f;
    [SerializeField] private float reelHeight = 180f;
    [SerializeField] private float minScrollSpeed = 500;
    [SerializeField] private float maxScrollSpeed = 800;
    [SerializeField] private float minSpinDuration = 2.5f;
    [SerializeField] private float maxSpinDuration = 3.5f;
    [SerializeField] private float minStopTime = 1.5f;
    [SerializeField] private float maxStopTime = 2.5f;

    public Animator VaultAnimator;
    private bool isSpinning = false;
    public bool isFreeSpinCompleted;
    public enum freeSpins { Six = 0, Nine = 1, Twelve = 2, Fifteen = 3 }
    public freeSpins freeSpinCount;
    public enum wildSpins { Fifty = 0, Hundred = 1, OneFifty = 2, TwoHundred = 3 }
    public wildSpins wildSpinCount;
    private Vector2 originalAnchoredPosition;
    public float delay = 0.6f;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        originalAnchoredPosition = freeSpinReelBox.anchoredPosition;
    }

    private void Start()
    {
        VaultAnimator = blindFeatureStartFrame.GetComponent<Animator>();
        leftVaultButton.onClick.AddListener(() => OnVaultClicked(true));
        rightVaultButton.onClick.AddListener(() => OnVaultClicked(false));
        freeSpinFeature.SetActive(false);
        HoldnLinkFeature.SetActive(false);
    }
    #endregion

    #region Public References

    public void StartBlindFeatureTransition()
    {
        isBlindFeatureCompleted = false;
        vaultAlreadyClicked = false;
        StartCoroutine(StartBlindFeature());
    }

    private float GetTargetY(int targetIndex)
    {
        return originalAnchoredPosition.y - ((targetIndex + 4) * slotHeight);
    }

    #endregion

    #region Game Transition
    private IEnumerator StartBlindFeature()
    {
        yield return new WaitUntil(() => CashVaultUIManager.Instance.winAnimationCompleted);
        yield return new WaitUntil(() => CashVaultSlotMachine.Instance.isSlotAnimationCompleted);

        CashVaultPaylineController.Instance.StopPaylines();
        CashVaultPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(1f);

        PopupAnimation(blindFeatureStartFrame, 1f, 1f, true);

        yield return new WaitForSeconds(1f);
    }
    private void OnVaultClicked(bool isLeftVault)
    {
        if (vaultAlreadyClicked)
            return;

        vaultAlreadyClicked = true;
        Button clickedButton = isLeftVault ? leftVaultButton : rightVaultButton;
        Button otherButton = isLeftVault ? rightVaultButton : leftVaultButton;

        clickedButton.interactable = false;
        otherButton.interactable = false;

        Transform vaultParent = clickedButton.transform;
        Debug.Log("Vault Clicked");
        if (isLeftVault)
        {
            VaultAnimator.SetBool("Left", true);
        }
        else
        {
            VaultAnimator.SetBool("Right", true);
        }

        StartCoroutine(ShowFeatureAfterDelay(vaultParent, delay, isLeftVault));
    }

    private IEnumerator ShowFeatureAfterDelay(Transform vaultParent, float delay, bool isLeftVault)
    {
        yield return new WaitForSeconds(delay);
        ShowFeatureOnVault(vaultParent, isLeftVault);
    }
    private void ShowFeatureOnVault(Transform vaultParent, bool isLeftVault)
    {
        if (vaultParent == null) return;

        float posX = isLeftVault ? -250f : 250f;
        Vector3 featurePos = new Vector3(posX,-70f, 0f);

        if (showFreeSpin)
        {
            Debug.Log("Lov Kumar 6");
            freeSpinFeature.SetActive(true);
            freeSpinFeature.transform.localPosition = featurePos;
            StartJackpot();
        }
        else if (showHoldnLink)
        {
            Debug.Log("Lov Kumar 7");
            HoldnLinkFeature.SetActive(true);
            HoldnLinkFeature.transform.localPosition = featurePos;
            Invoke(nameof(CompleteBlindFeature), 3.5f);
        }
    }
    private void CompleteBlindFeature()
    {
        if (isBlindFeatureCompleted) return;

        isBlindFeatureCompleted = true;
        PopupAnimation(blindFeatureStartFrame, 0f, 1f, false);
        VaultAnimator.SetBool("Left", false); 
        VaultAnimator.SetBool("Right", false);
        OnBlindFeatureCompleted?.Invoke();
    }
    #endregion

    #region FreeSpins & Wilds Reel

    [ContextMenu("Start Jackpot")]
    public void StartJackpot()
    {
        isFreeSpinCompleted = false;
        StartCoroutine(ShowJackpot());
    }
    private IEnumerator ShowJackpot()
    {
        yield return new WaitForSeconds(2f);

        //if (CashVaultSlotMachine.Instance.freeSpinCount == 6)
        //{
        //    freeSpinCount = freeSpins.Six;
        //}
        //else if(CashVaultSlotMachine.Instance.freeSpinCount == 9)
        //{
        //    freeSpinCount = freeSpins.Nine;
        //}
        //else if (CashVaultSlotMachine.Instance.freeSpinCount == 12)
        //{
        //    freeSpinCount = freeSpins.Twelve;
        //}
        //else
        //{
        //    freeSpinCount = freeSpins.Fifteen;
        //}
        //if (CashVaultSlotMachine.Instance.wildCount == 50)
        //{
        //    wildSpinCount = wildSpins.Fifty;
        //}
        //else if (CashVaultSlotMachine.Instance.freeSpinCount == 9)
        //{
        //    wildSpinCount = wildSpins.Hundred;
        //}
        //else if (CashVaultSlotMachine.Instance.freeSpinCount == 12)
        //{
        //    wildSpinCount = wildSpins.OneFifty;
        //}
        //else
        //{
        //    wildSpinCount = wildSpins.TwoHundred;
        //}

        StartSpin(freeSpinReelBox, (int)freeSpinCount);

        yield return new WaitUntil(() => !IsSpinning());
        yield return new WaitForSeconds(2.5f);

        StartSpin(wildReelBox, (int)wildSpinCount);

        yield return new WaitUntil(() => !IsSpinning());
        isFreeSpinCompleted = true;
        CompleteBlindFeature();
    }
    public void StartSpin(RectTransform reelBox, int resultIndex)
    {
        if (!isSpinning)
            StartCoroutine(SpinRoutine(reelBox, resultIndex));
    }
    IEnumerator SpinRoutine(RectTransform reelBox, int targetIndex)
    {
        float spinDuration = Random.Range(minSpinDuration, maxSpinDuration);
        float scrollSpeed = Random.Range(minScrollSpeed, maxScrollSpeed);

        isSpinning = true;

        reelBox.anchoredPosition = originalAnchoredPosition;

        float currentY = originalAnchoredPosition.y;
        float elapsed = 0f;

        float loopHeight = 4 * slotHeight;

        while (elapsed < spinDuration)
        {
            currentY -= scrollSpeed * Time.deltaTime;

            if (currentY <= originalAnchoredPosition.y - loopHeight)
            {
                currentY += loopHeight;
            }

            reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, currentY);

            elapsed += Time.deltaTime;
            yield return null;
        }

        float stopTime = Random.Range(minStopTime, maxStopTime);
        float t = 0f;
        float startY = reelBox.anchoredPosition.y;
        float targetY = GetTargetY(targetIndex);

        while (targetY > startY)
        {
            targetY -= loopHeight;
        }

        while (t < stopTime)
        {
            float eased = EaseOutCubic(t / stopTime);
            float y = Mathf.Lerp(startY, targetY, eased);

            reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, y);

            t += Time.deltaTime;
            yield return null;
        }

        reelBox.anchoredPosition = new Vector2(originalAnchoredPosition.x, targetY);
        isSpinning = false;
    }

    #endregion

    #region Helper Funcitons

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }

    public bool IsSpinning() => isSpinning;

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }
    #endregion
}