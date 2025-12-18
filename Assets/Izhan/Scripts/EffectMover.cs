using System.Collections;
using DG.Tweening;
using UnityEngine;

public class EffectMover : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 100f;
    public float stopDistance = 0.1f;
    public float delayBeforeMove = 1f;

    private bool moving = false;

    public void Initialize(Transform targetTransform)
    {
        target = targetTransform;
        StartCoroutine(DelayedStart());
        
    }
    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(delayBeforeMove);
        moving = true;
    }

    void Update()
    {
        if (!moving || target == null) return;

        // Get positions on the 2D plane
        Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetPos2D = new Vector2(target.position.x, target.position.y);

        // Move toward the target on the X/Y plane only
        Vector2 newPos2D = Vector2.MoveTowards(currentPos2D, targetPos2D, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(newPos2D.x, newPos2D.y, transform.position.z); // keep original Z

        if (Vector2.Distance(newPos2D, targetPos2D) <= stopDistance)
        {
            moving = false;
            TriggerTargetEffect();
            Destroy(gameObject); // Destroy the click effect after arriving
        }
    }

    private void TriggerTargetEffect()
    {
        if (target == null) return;

        // Play ParticleSystem if available
        ParticleSystem ps = target.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Debug.Log("[EffectMover] ParticleSystem played on target.");
        }

        // DOTween pulse scale
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        target.DOScale(targetScale, 0.2f)
              .SetEase(Ease.OutQuad)
              .OnComplete(() =>
              {
                  target.DOScale(originalScale, 0.2f)
                        .SetEase(Ease.InQuad);
              });

        Debug.Log("[EffectMover] Pulse scale animation with DOTween triggered.");
    }
}
