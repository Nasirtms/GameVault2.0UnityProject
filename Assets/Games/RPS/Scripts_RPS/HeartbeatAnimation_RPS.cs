using UnityEngine;
using DG.Tweening; // Make sure you have DOTween imported

public class HeartbeatAnimation : MonoBehaviour
{
    public float scaleUpFactor = 1.2f;     // How much to scale up at the start
    public float heartbeatScale = 1.3f;    // Peak scale during heartbeat
    public float scaleUpTime = 0.3f;       // Time to scale up initially
    public float beatDuration = 0.15f;     // Time for each beat up/down
    public int beatCount = 2;              // Number of beats
    public float returnTime = 0.3f;        // Time to return to normal scale

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    [ContextMenu("Play Heartbeat Animation")]
    public void PlayHeartbeat(GameObject target)
    {
        Vector3 originalScale = target.transform.localScale;

        target.transform.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(target.transform.DOScale(originalScale * scaleUpFactor, scaleUpTime).SetEase(Ease.OutQuad));
        for (int i = 0; i < beatCount; i++)
        {
            seq.Append(target.transform.DOScale(originalScale * heartbeatScale, beatDuration).SetEase(Ease.OutQuad));
            seq.Append(target.transform.DOScale(originalScale * scaleUpFactor, beatDuration).SetEase(Ease.InQuad));
        }
        seq.Append(target.transform.DOScale(originalScale, returnTime).SetEase(Ease.InOutQuad));
    }
}
