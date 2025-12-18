using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum OctagonKenoButtonState { Unselected, Selected, Drawn, Hit }

public class SuperBallKenoButton : MonoBehaviour
{
    public int number;
    public TMP_Text numberText; 
    public TMP_Text numberTextShadow; 
    public Button button;
    public Image targetGraphic;
    public Animator animator;


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
        //number = num;
        button.onClick.RemoveAllListeners();
        //button.onClick.AddListener(() => SuperBallKeno.Instance.OnNumberClicked(number));
    }
    public KenoButtonState _state;
    public void SetState(KenoButtonState state)
    {
        _state = state;
        // stop animation unless it's a hit
        //Debug.Log($"1 Setting state to {state} for button with number {number}");
        if (animator != null)
        {
            //animator.ResetTrigger("LHit");
            animator.enabled = false;
        }

        switch (state)
        {
            case KenoButtonState.Unselected:
                if (targetGraphic != null)
                {
                    //KenoAudioManager.Instance.PlaySound(KenoSound.NumberSelect);
                    targetGraphic.sprite = unselectedSprite;
                }
                numberText.color = Color.white;
                isOn = true;
                break;

            case KenoButtonState.Selected:
                //KenoAudioManager.Instance.PlaySound(KenoSound.NumberSelect);
                if (targetGraphic != null) targetGraphic.sprite = selectedSprite;
                numberText.color = Color.red;
                isOn = false;
                break;

            case KenoButtonState.Drawn:
                if (targetGraphic != null) targetGraphic.sprite = drawnSprite;
                break;

            case KenoButtonState.Hit:
                SuperBallKeno.Instance.PlaySound("Each_Hit");
                if(animator == null) {  GetComponent<Animator>(); }

                if (animator != null)
                {
                    //Debug.Log($"3 Setting state to {state} for button with number {number}");
                    animator.enabled = true;
                }
                break;
        }
    }
}
