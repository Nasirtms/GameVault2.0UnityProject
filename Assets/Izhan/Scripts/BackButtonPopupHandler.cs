using DG.Tweening;
using UnityEngine;

public class BackButtonPopupHandler : MonoBehaviour
{
    public GameObject backButtonPopup;
    private Animator popupAnimator;
    private bool isPopupActive = false;
    void Update()
    {
#if UNITY_ANDROID
        // Detect back button press (Escape key in Editor and on Android)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            backbutton();
        }
        else
        {
            isPopupActive = false;
        }
#endif
    }

    public void backbutton()
    {
        Debug.Log("Escape (Back) button detected");

        if (backButtonPopup != null)
        {
            if (!isPopupActive)
            {
                backButtonPopup.SetActive(true);
                Debug.Log("Activating popup and starting animation");
                DoTweenAnim(TweenType.Panel, backButtonPopup.transform.gameObject, 82f, 0.3f);
                isPopupActive = true;
                
            }
        }
        else
        {
            Debug.LogWarning("No popup assigned!");
        }
    }

    public void DoTweenAnim(TweenType type, GameObject obj, float scale, float duration)
    {
        if (obj == null) return;

        obj.transform.DOKill();
        switch (type) 
        {
            case TweenType.Panel:
                obj.transform.localScale = Vector3.one * 0.5f;
                obj.transform.DOScale(scale, duration * 1.2f)
                    .SetEase(Ease.OutBack);
                break;
        }
         
    }
    public enum TweenType { Panel }   
}
