using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class QuickHitVolcanoQuickPickItem : MonoBehaviour
{
    [SerializeField] private Image hiddenImage;
    [SerializeField] private Image revealedImage;
    [SerializeField] private ParticleSystem clickParticle;
    private QuickHitVolcanoQuickPickSymbolType symbolType;
    private Sprite revealedSprite;
    private bool isRevealed = false;

    public bool IsRevealed => isRevealed;

    public void OnItemClick()
    {
        if (isRevealed || QuickHitVolcanoQuickPickGameManager.Instance.IsGameEnded) return;

        QuickHitVolcanoQuickPickGameManager.Instance.OnItemClicked(this);
    }

    public void ForceReveal(QuickHitVolcanoQuickPickSymbolType type, Sprite sprite, bool triggerLogic = true)
    {
        if (isRevealed) return;

        isRevealed = true;
        symbolType = type;
        revealedSprite = sprite;

        // Start particle effect right away
        if (triggerLogic)
        {
            clickParticle.Play();
        }

        // Animate hidden image scale down
        hiddenImage.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            hiddenImage.gameObject.SetActive(false);

            // Prepare revealed image
            revealedImage.sprite = revealedSprite;
            revealedImage.transform.localScale = Vector3.zero;
            revealedImage.gameObject.SetActive(true);

            // Animate revealed image scale up
            revealedImage.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        });

        // Trigger logic immediately (optional: delay if needed)
        if (triggerLogic)
        {
            QuickHitVolcanoQuickPickGameManager.Instance.OnItemForceRevealed(this, symbolType);
        }
    }

    public void ResetItem()
    {
        isRevealed = false;
        hiddenImage.gameObject.SetActive(true);
        revealedImage.gameObject.SetActive(false);
    }
}
