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
    private Tween scaleTween;
    private Transform child0;
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

        if (child0 == null)
        {
            child0 = transform.GetChild(0);
        }

        if (sr == null)
            sr = child.GetComponent<Image>();

        // Kill existing tweens
        colorTween?.Kill();
        scaleTween?.Kill();

        float hue = 0f;

        float currentDuration = FruitSlotMachine.Instance.isFreeGame ? duration * 0.15f : duration;

        colorTween = DOTween.To(() => hue, x =>
        {
            hue = x;
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);
            sr.color = newColor;
        },
        1f, currentDuration)
        .SetLoops(-1, LoopType.Restart);

        child0.localScale = Vector3.one;

        scaleTween = child0.DOScale(0.7f, 0.4f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopAnimation()
    {
        colorTween?.Kill();
        colorTween = null;

        scaleTween?.Kill();
        scaleTween = null;

        if (sr != null)
        {
            sr.color = new Color32(0x00, 0x7C, 0xFF, 0xFF);
        }

        if (child0 != null)
        {
            // smooth reset instead of snap
            child0.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);
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

