// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeadTailGame
{
    public class HeadTailUIManager : MonoBehaviour
    {
        public static HeadTailUIManager Instance;

        [Header("Buttons")]
        [SerializeField] private Button headsButton;
        [SerializeField] private Button tailsButton;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button closeInfoButton;

        [Header("UI")]
        [SerializeField] private TMP_Text betLabel;          // e.g. "Bet: 10"
        [SerializeField] private GameObject rulesPanel;  // set inactive by default
        [SerializeField] private TMP_Text coins;
        [SerializeField] private TMP_Text winAmount;
        [SerializeField] private GameObject winAmountParent;

        public event Action OnRulesOpened;
        public event Action OnRulesClosed;
        public Button HeadsButton => headsButton;
        public Button TailsButton => tailsButton;
        public Button IncreaseBetButton => increaseBetButton;
        public Button DecreaseBetButton => decreaseBetButton;

        void Awake()
        {
            if (Instance != null)
            {
                return;
            }
            else
            {
                Instance = this;
            }
                
            if (rulesPanel != null) rulesPanel.SetActive(false);

            if (infoButton != null) infoButton.onClick.AddListener(() =>
            {
                if (rulesPanel != null) rulesPanel.SetActive(true);
                OnRulesOpened?.Invoke();
            });

            if (closeInfoButton != null) closeInfoButton.onClick.AddListener(() =>
            {
                if (rulesPanel != null) rulesPanel.SetActive(false);
                OnRulesClosed?.Invoke();
            });

            if (homeButton != null) homeButton.onClick.AddListener(ExitGame);
            if (headsButton != null)
                headsButton.onClick.AddListener(HideWinAmountBanner);

            if (tailsButton != null)
                tailsButton.onClick.AddListener(HideWinAmountBanner);

        }
        private void Start()
        {
            UpdateCoins();
            GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
            UserManager.Instance.UpdateGameCoins += UpdateCoins;
        }
        private void OnDestroy()
        {
            UserManager.Instance.UpdateGameCoins -= UpdateCoins;
        }
        public void UpdateCoins()
        {
            if (UserManager.Instance != null)
            {
                coins.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
            }
        }
        public void SetBetText(float amount)
        {
            if (betLabel != null) betLabel.text = amount.ToString("0.00");
        }

        public void UpdateWinAmount(double betAmount, bool isWin)
        {
            if (winAmount == null || winAmountParent == null) return;
            RectTransform rt = winAmountParent.GetComponent<RectTransform>();
            if (rt != null) DOTween.Kill(rt);

            if (isWin)
            {
                HeadTailSoundManager.Instance.PlaySFX("Win");
                double payout = betAmount * 1.98;
                winAmount.text = $"Win: {payout:F2}";
            }
            else
            {
                HeadTailSoundManager.Instance.PlaySFX("Lose");
                winAmount.text = "You Lose!";
            }
            winAmountParent.SetActive(true);

            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -100f);

                // Slide up to visible position
                rt.DOAnchorPosY(0f, 0.6f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    // After delay, slide back down and deactivate
                     rt.DOAnchorPosY(-100f, 0.5f)
                          .SetEase(Ease.InCubic)
                          .SetDelay(1.5f)
                          .OnComplete(() =>{winAmountParent.SetActive(false);});
                });
            }
        }

        public void SetAllButtonsInteractable(bool interactable)
        {
            if (headsButton) headsButton.interactable = interactable;
            if (tailsButton) tailsButton.interactable = interactable;
            if (increaseBetButton) increaseBetButton.interactable = interactable;
            if (decreaseBetButton) decreaseBetButton.interactable = interactable;
            if (infoButton) infoButton.interactable = interactable;
            if (homeButton) homeButton.interactable = interactable;
            if (closeInfoButton) closeInfoButton.interactable = interactable;
        }

        public void SetAllButtonsInteractableExcept(Button exceptButton)
        {
            // others OFF
            if (headsButton && headsButton != exceptButton) headsButton.interactable = false;
            if (tailsButton && tailsButton != exceptButton) tailsButton.interactable = false;
            if (increaseBetButton && increaseBetButton != exceptButton) increaseBetButton.interactable = false;
            if (decreaseBetButton && decreaseBetButton != exceptButton) decreaseBetButton.interactable = false;
            if (infoButton && infoButton != exceptButton) infoButton.interactable = false;
            if (homeButton && homeButton != exceptButton) homeButton.interactable = false;
            if (closeInfoButton && closeInfoButton != exceptButton) closeInfoButton.interactable = false;

            // the “except” stays ON (if provided)
            if (exceptButton) exceptButton.interactable = true;
        }

        private void ExitGame()
        {
            if (UserManager.Instance != null)
            {
                UserManager.Instance.StartUpdateCanAddCoin(true);
            }
            SceneManager.LoadScene("Main");
        }
        public void HideWinAmountBanner()
        {
            if (winAmountParent == null) return;

            RectTransform rt = winAmountParent.GetComponent<RectTransform>();
            if (rt != null)
            {
                DOTween.Kill(rt); // stop any active tweens
                rt.DOAnchorPosY(-100f, 0.3f).SetEase(Ease.InCubic)
                    .OnComplete(() => winAmountParent.SetActive(false));
            }
            else
            {
                winAmountParent.SetActive(false);
            }
        }
    }
}
