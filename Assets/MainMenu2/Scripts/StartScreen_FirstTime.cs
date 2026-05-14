using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class StartScreen_FirstTime : MonoBehaviour
    {
        public Animator startScreenAnimator;
        public UIPanel canvas;
        public Button rightButton;
        public Button leftButton;
        public Button selectButton;

        public float doneAnimationDuration = 2;

        public Vector3 moveTargetPosition = new Vector3(-13.06f, -3.81f, 0f);

        private void Awake()
        {
            rightButton.onClick.AddListener(RightPressed);
            leftButton.onClick.AddListener(LeftPressed);
            selectButton.onClick.AddListener(SelectPressed);
        }

        public void PlayIdleAnimation()
        {
            startScreenAnimator.Play("Avatar_Building");
        }

        public void PlayDoneAnimation()
        {
            startScreenAnimator.CrossFade("Avatar_Transition",0.01f);
        }

        void RightPressed()
        {
            MainMenuManager.instance.StartScreen_AvatarSelectRightButton();
        }

        void LeftPressed()
        {
            MainMenuManager.instance.StartScreen_AvatarSelectLeftButton();
        }

        void SelectPressed()
        {
            canvas.panel.interactable = false;
            canvas.ClosePanel(0.4f);

            DOVirtual.DelayedCall(0.4f, () =>
            {
                MainMenuManager.instance.StartScreen_Done();
            });
        }
    }
}