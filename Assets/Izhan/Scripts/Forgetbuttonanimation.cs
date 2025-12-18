using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Forgetbuttonanimation : MonoBehaviour
{
    public Button targetButton;
    public Vector3 clickedScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float scaleDuration = 0.2f;

    private Vector3 originalScale;

    void Start()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        originalScale = targetButton.transform.localScale;
        targetButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleButton());
    }

    System.Collections.IEnumerator ScaleButton()
    {
        // Scale up
        yield return ScaleTo(clickedScale);
        // Scale back down
        yield return ScaleTo(originalScale);
    }

    System.Collections.IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = targetButton.transform.localScale;
        float elapsed = 0;

        while (elapsed < scaleDuration)
        {
            targetButton.transform.localScale = Vector3.Lerp(start, target, elapsed / scaleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetButton.transform.localScale = target;
    }
}
