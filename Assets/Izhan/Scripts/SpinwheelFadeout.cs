using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SpinwheelFadeout : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Transform objectToMove;
    public Transform moveTarget;
    public float fadeDuration = 3f;
    public GameObject ParticleParent;
    private ParticleSystem particleSystem;
    private float initialEmissionRate;


    public void StartFadeMoveAndReduce()
    {
        // Get ParticleSystem component from objectToMove
        ParticleParent.SetActive(false);
        particleSystem = objectToMove.GetComponentInChildren<ParticleSystem>();

        if (particleSystem != null)
        {
            var emission = particleSystem.emission;
            initialEmissionRate = emission.rateOverTime.constant;
        }

        StartCoroutine(FadeOutAndMove());
    }

    private IEnumerator FadeOutAndMove()
    {
        CylindricalUIWarpSwipe.isDragable = false;
        MainMenuUIManager.Instance.clickEffectPrefab.SetActive(false);
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startPos = objectToMove.position;
        Vector3 targetPos = moveTarget.position;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade out CanvasGroup
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            // Move object
            objectToMove.position = Vector3.Lerp(startPos, targetPos, t);

            // Reduce particle emission rate
            if (particleSystem != null)
            {
                var emission = particleSystem.emission;
                emission.rateOverTime = Mathf.Lerp(initialEmissionRate, 0f, t);
            }

            yield return null;
        }

        // Finalize all values
        canvasGroup.alpha = 0f;
        objectToMove.position = targetPos;
        yield return new WaitForSeconds(1f);
        canvasGroup.gameObject.SetActive(false);
        MainMenuUIManager.Instance.clickEffectPrefab.SetActive(true);
        if (MainMenuUIManager.Instance.isDragable())
            CylindricalUIWarpSwipe.isDragable = true;
        else
            CylindricalUIWarpSwipe.isDragable = false;
    }
}
