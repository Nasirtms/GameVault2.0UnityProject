using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CategoryButtonEffect : MonoBehaviour
{
    [Header("Visual Elements")]
    public Image background;
    public Image icon;

    [Header("Label")]
    public TMP_Text label;

    [Header("Sprites")]
    public Sprite activeBackground;
    public Sprite inactiveBackground;

    [Header("Animation")]
    public float iconMoveDistance = 10f;
    public float iconMoveDuration = 0.5f;

    private Tween iconTween;

    public void SetActiveState(bool isActive)
    {
        if (!gameObject.name.Equals("New"))
        {
            // 1. Background
            background.sprite = isActive ? activeBackground : inactiveBackground;

            // 2. Icon Animation
            if (iconTween != null) iconTween.Kill();

            if (isActive)
            {
                iconTween = icon.rectTransform
                    .DOAnchorPosY(icon.rectTransform.anchoredPosition.y + iconMoveDistance, iconMoveDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                icon.rectTransform.anchoredPosition = new Vector2(icon.rectTransform.anchoredPosition.x, 25f);
            }
        }
    }
}
