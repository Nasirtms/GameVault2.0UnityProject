using UnityEngine;

[ExecuteAlways]
public class GlobeUVScroller : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string textureProperty = "_MainTex";
    [SerializeField] private float scrollSpeedX = -0.1f;
    [SerializeField] private float scrollSpeedY = 0f;

    private Material runtimeMaterial;
    private Vector2 currentOffset;

    private void OnEnable()
    {
        SetupMaterialInstance();
        UpdateOffsetImmediate();
    }

    private void OnValidate()
    {
        SetupMaterialInstance();
        UpdateOffsetImmediate();
    }

    private void Update()
    {
        if (runtimeMaterial == null)
            SetupMaterialInstance();

        if (runtimeMaterial == null)
            return;

        currentOffset.x += scrollSpeedX * Time.deltaTime;
        currentOffset.y += scrollSpeedY * Time.deltaTime;

        runtimeMaterial.SetTextureOffset(textureProperty, currentOffset);
    }

    private void SetupMaterialInstance()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
            return;

        if (Application.isPlaying)
        {
            runtimeMaterial = targetRenderer.material;
        }
        else
        {
            runtimeMaterial = targetRenderer.sharedMaterial;
        }
    }

    private void UpdateOffsetImmediate()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetTextureOffset(textureProperty, currentOffset);
    }
}