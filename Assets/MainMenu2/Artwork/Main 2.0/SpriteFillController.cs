using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFillController : MonoBehaviour
{
    public enum FillMethod
    {
        Horizontal = 0,
        Vertical = 1,
        Radial90 = 2,
        Radial180 = 3,
        Radial360 = 4
    }

    [Range(0f, 1f)]
    public float fillAmount = 1f;

    public FillMethod fillMethod = FillMethod.Horizontal;
    public int fillOrigin = 0;
    public bool clockwise = true;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock mpb;

    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int FillMethodID = Shader.PropertyToID("_FillMethod");
    private static readonly int FillOriginID = Shader.PropertyToID("_FillOrigin");
    private static readonly int ClockwiseID = Shader.PropertyToID("_Clockwise");
    private static readonly int SpriteRectID = Shader.PropertyToID("_SpriteRect");

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        Apply();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        fillAmount = Mathf.Clamp01(fillAmount);
        Apply();
    }

    public void Apply()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        spriteRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(FillAmountID, fillAmount);
        mpb.SetFloat(FillMethodID, (float)fillMethod);
        mpb.SetFloat(FillOriginID, fillOrigin);
        mpb.SetFloat(ClockwiseID, clockwise ? 1f : 0f);

        Bounds b = spriteRenderer.sprite.bounds;
        Vector4 spriteRect = new Vector4(b.min.x, b.min.y, b.max.x, b.max.y);
        mpb.SetVector(SpriteRectID, spriteRect);

        spriteRenderer.SetPropertyBlock(mpb);
    }

    [ExecuteInEditMode]
    void Update()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        fillAmount = Mathf.Clamp01(fillAmount);
        Apply();
    }
}