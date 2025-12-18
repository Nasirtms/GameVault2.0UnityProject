using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class UIPanel : MonoBehaviour
    {
        public CanvasGroup panel;

        public void OpenPanel(float transitionDuration)
        {
            panel.gameObject.SetActive(true);
            panel.DOFade(1, transitionDuration).SetEase(Ease.OutSine);
        }

        public void ClosePanel(float transitionDuration)
        {
            panel.DOFade(0, transitionDuration).SetEase(Ease.InSine).OnComplete(()=>panel.gameObject.SetActive(false));
        }
    }
}