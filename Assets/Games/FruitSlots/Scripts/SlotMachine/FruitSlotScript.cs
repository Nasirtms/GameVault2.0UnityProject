using DG.Tweening;
using Sequence = DG.Tweening.Sequence;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FruitSlotScript : MonoBehaviour
{
    public FruitSlotType type;
    public int index;
    public bool isResultSet = false;
    public int paylineNumber;
    public int ReelIndex;
    public int RowIndex;

    public List<int> paylineNumberList = new List<int>();

    [SerializeField] private float duration = 1f;
    private Tween colorTween;
    private Image sr;
    private Transform child;
    //private Image _background;
    private RectTransform _rectTransform;
    private FruitSlotReelScript _parent;
    //private Canvas _canvas;

    [SerializeField] private Image icon; // Optional: drag icon Image in inspector (for animated overlay)
    public FruitSlotResource currentResource;

    //private Image Background => _background ??= GetComponent<Image>();


    private void OnEnable()
    {
        if (FruitSlotMachine.Instance != null)
            FruitSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDisable()
    {
        if (FruitSlotMachine.Instance != null)
            FruitSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    private void HandleStart()
    {
        //Debug.Log("Coming From Slot Script " + gameObject.name);
        StopAllCoroutines();
    }
    public void Initialize(FruitSlotReelScript parentReel, int reelIndex, int rowIndex)
    {
        this._parent = parentReel;
        this.ReelIndex = reelIndex;
        this.RowIndex = rowIndex;
        this.index = rowIndex; // optional if you use `index` elsewhere
        _rectTransform = GetComponent<RectTransform>();

        ToggleSlotBorder(false);
        ShowRandomVisual();
    }


    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void SetVisibility(bool status)
    {
        gameObject.SetActive(status);
    }

    public void SetType(FruitSlotResource newType)
    {
        this.currentResource = newType;
        this.type = newType.type;

        icon.sprite = newType.background;
    }
    public void ShowRandomVisual()
    {
        FruitSlotResource random;
        string parentName = gameObject.transform.parent.name;
        do
        {
            random = GetRandomSlot();
        }
        while (
        (parentName == "Reels0" || parentName == "Reels4") &&
        (random.type == FruitSlotType.BONUS || random.type == FruitSlotType.WILD)
        );

        SetType(random);

    }



    FruitSlotResource GetRandomSlot()
    {
        var random = FruitSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, FruitSlotMachine.Instance.settings.resourcesList.Count)];

        return random;
    }
    public void AnimateSymbol(float duration = 0.25f)
    {
        if (_rectTransform == null) return;

        _rectTransform.localScale = Vector3.zero;
    }

    public void ToggleSlotBorder(bool visible)
    {
        //if (border != null)
        //{
        //    border.SetActive(visible);
        //}
        //else
        //{
        //    Debug.LogWarning($"❌ Slot {gameObject.name} missing border reference!");
        //}
    }


    public void ShowUniqueRandomVisual(HashSet<FruitSlotType> excludedTypes)
    {
        FruitSlotResource random;
        string parentName = gameObject.transform.parent.name;

        int safety = 100;

        do
        {
            random = GetRandomSlot();
            safety--;
        }
        while (
            safety > 0 &&
            (
                excludedTypes.Contains(random.type) ||
                ((parentName == "Reels0" || parentName == "Reels4") &&
                 (random.type == FruitSlotType.BONUS || random.type == FruitSlotType.WILD))
            )
        );

        excludedTypes.Add(random.type);
        SetType(random);
    }


    //public void PlayAnimation()
    //{
    //    if (child == null)
    //    {
    //        child = transform.GetChild(1);
    //    }

    //    child.gameObject.SetActive(true);

    //    if (sr == null)
    //        sr = child.GetComponent<Image>();

    //    // kill any existing tween
    //    colorTween?.Kill();

    //    float hue = 0f;

    //    colorTween = DOTween.To(() => hue, x =>
    //    {
    //        hue = x;
    //        Color newColor = Color.HSVToRGB(hue, 1f, 1f); // full saturation & brightness
    //        sr.color = newColor;
    //    },
    //    1f, duration) // go from hue=0 → hue=1 (360° on color wheel)
    //    .SetLoops(-1, LoopType.Restart); // infinite loop
    //}
    public void PlayAnimation()
    {
        if (child == null)
        {
            child = transform.GetChild(1);
        }

        child.gameObject.SetActive(true);

        if (sr == null)
            sr = child.GetComponent<Image>();

        // Kill any existing tween
        colorTween?.Kill();

        float hue = 0f;

        // 🔹 Adjust animation speed based on Free Spin state
        float currentDuration = FruitSlotMachine.Instance.isFreeGame ? duration * 0.15f : duration;
        // 0.6x = 40% faster (tweak if needed, e.g., 0.5f for 2x speed)

        colorTween = DOTween.To(() => hue, x =>
        {
            hue = x;
            Color newColor = Color.HSVToRGB(hue, 1f, 1f); // full saturation & brightness
            sr.color = newColor;
        },
        1f, currentDuration) // use adjusted duration
        .SetLoops(-1, LoopType.Restart); // infinite loop
    }

    public void StopAnimation()
    {
        colorTween?.Kill();
        colorTween = null;

        if (sr != null)
        {
            sr.color = new Color32(0x00, 0x7C, 0xFF, 0xFF);
        }

        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        colorTween?.Kill();
    }
}

