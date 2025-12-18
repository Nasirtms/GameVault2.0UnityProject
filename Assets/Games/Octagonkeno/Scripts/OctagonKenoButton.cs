using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum KenoButtonState { Unselected, Selected, Drawn, Hit }

public class OctagonKenoButton : MonoBehaviour
{
    public int number;
    public TMP_Text numberText;
    public Button button;
    public Image targetGraphic;
    public Animator animator;
    public KenoButtonState _state;
    public Transform hitimage;

    public Sprite unselectedSprite;
    public Sprite selectedSprite;
    public Sprite drawnSprite;

    public bool isOn = true;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (targetGraphic == null) targetGraphic = GetComponent<Image>();
    }

    public void Initialize(int num)
    {
        button.onClick.RemoveAllListeners();
    }
    public void PlayHitSound()
    {
        OctagonKeno.Instance.PlaySound("Each_Hit");
    }

    public void SetState(KenoButtonState state)
    {
        _state = state;
        //Debug.Log($"2 Setting state to {state} for button with number {number}");

        switch (state)
        {
            case KenoButtonState.Unselected:
                //Debug.Log($"Setting state - 1");
               // hitimage.transform.localScale = Vector3.zero;
                if (targetGraphic != null)
                    targetGraphic.sprite = unselectedSprite;

                numberText.color = Color.gray;
                isOn = true;

                // Only disable animator if not needed anymore
                if (animator != null)
                    animator.enabled = false;
                break;

            case KenoButtonState.Selected:
                //Debug.Log($"Setting state -2");
               // hitimage.transform.localScale = Vector3.zero;
                if (targetGraphic != null)
                    targetGraphic.sprite = selectedSprite;

                numberText.color = Color.white;
                isOn = false;

                if (animator != null)
                    animator.enabled = false;
                break;

            case KenoButtonState.Drawn:
                //Debug.Log($"Setting state -3");
         
                // hitimage.transform.localScale = Vector3.zero;
                if (targetGraphic != null)
                    targetGraphic.sprite = drawnSprite;

                numberText.color = Color.red;

                if (animator != null)
                    animator.enabled = false;
                break;

            case KenoButtonState.Hit:
                //Debug.Log($"Setting state -4");
                if (animator == null)
                    animator = GetComponent<Animator>();

                if (animator != null)
                {
                    animator.enabled = true;      // enable before triggering
                    animator.ResetTrigger("StillHit");
                    animator.SetTrigger("Hit");
                }
                break;
        }
    }

    public void OnAnimationComplete()
    {
        OctagonKeno.Instance?.OnDrawAnimationComplete();
    }

}
