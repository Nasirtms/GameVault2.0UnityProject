using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class UILoadingPanel : UIPanel
    {
        public Image loadingBarFill;

        public override void OpenPanel(float transitionDuration)
        {
            panel.gameObject.SetActive(true);
            panel.DOFade(1, transitionDuration).SetEase(Ease.OutSine);
            loadingBarFill.transform.localScale = new Vector3(0, 1, 1);
        }

        public void OpenPanel(float transitionDuration, float fakeLoadingTime)
        {
            panel.gameObject.SetActive(true);
            panel.DOFade(1, transitionDuration).SetEase(Ease.OutSine);
            if (fakeLoadingTime > -1)
            {
                loadingBarFill.transform.localScale = new Vector3(0, 1, 1);
                loadingBarFill.transform.DOScaleX(1, fakeLoadingTime).SetEase((Ease)Random.Range(2, 23));
            }
        }

        public float GetLoadingBarValue()
        {
            return loadingBarFill.transform.localScale.x;
        }

        public void SetLoadingBarValue(float value)
        {
            loadingBarFill.transform.localScale = new Vector3(value, 1, 1);
        }
    }
}