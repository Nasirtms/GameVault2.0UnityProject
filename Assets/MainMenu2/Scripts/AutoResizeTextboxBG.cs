using TMPro;
using UnityEngine;

public class AutoResizeTextboxBG : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private RectTransform textboxBG;
    [SerializeField] private float padding = 20f;
    [SerializeField] private float minSize = 70f;
    [SerializeField] private float maxSize = 600f;

    private float targetSizeY;

    private void Update()
    {
        float height = text.preferredHeight + padding;

        targetSizeY = height;
        targetSizeY = Mathf.Clamp(targetSizeY, minSize, maxSize);

        Vector2 size = textboxBG.sizeDelta;
        size.y = Mathf.Lerp(size.y, targetSizeY, Time.deltaTime * 20);
        textboxBG.sizeDelta = size;
    }
}