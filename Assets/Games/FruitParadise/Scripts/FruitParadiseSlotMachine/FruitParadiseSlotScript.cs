using DG.Tweening;
using Sequence = DG.Tweening.Sequence;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FruitParadiseSlotScript : MonoBehaviour
{
    public FruitParadiseSlotType type;
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
    private RectTransform _rectTransform;
    private FruitParadiseReelScript _parent;

    [SerializeField] private Image icon; 
    public FruitParadiseSlotResource currentResource;

    private void OnEnable()
    {
        if (FruitParadiseSlotMachine.Instance != null)
            FruitParadiseSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDisable()
    {
        if (FruitParadiseSlotMachine.Instance != null)
            FruitParadiseSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    private void HandleStart()
    {
        //Debug.Log("Coming From Slot Script " + gameObject.name);
        StopAllCoroutines();
    }

    public void Initialize(FruitParadiseReelScript parentReel, int reelIndex, int rowIndex)
    {
        this._parent = parentReel;
        this.ReelIndex = reelIndex;
        this.RowIndex = rowIndex;
        this.index = rowIndex; 
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

    public void SetType(FruitParadiseSlotResource newType)
    {
        this.currentResource = newType;
        this.type = newType.type;

        icon.sprite = newType.background;
    }
    public void ShowRandomVisual()
    {
        FruitParadiseSlotResource random;
        string parentName = gameObject.transform.parent.name;
        do
        {
            random = GetRandomSlot();
        }
        while (
        (parentName == "Reels0" || parentName == "Reels4") &&
        (random.type == FruitParadiseSlotType.Bonus || random.type == FruitParadiseSlotType.Wild)
        );

        SetType(random);

    }

    FruitParadiseSlotResource GetRandomSlot()
    {
        var random = FruitParadiseSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, FruitParadiseSlotMachine.Instance.settings.resourcesList.Count)];

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


    public void ShowUniqueRandomVisual(HashSet<FruitParadiseSlotType> excludedTypes)
    {
        FruitParadiseSlotResource random;
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
                 (random.type == FruitParadiseSlotType.Bonus || random.type == FruitParadiseSlotType.Wild))
            )
        );

        excludedTypes.Add(random.type);
        SetType(random);
    }


    public void PlayAnimation()
    {
        if (child == null)
        {
            child = transform.GetChild(2);
        }

        child.gameObject.SetActive(true);

        if (sr == null)
            sr = child.GetComponent<Image>();

        colorTween?.Kill();

        float hue = 1f;

        colorTween = DOTween.To(() => hue, x =>
        {

        },
        1f, duration) // go from hue=0 → hue=1 (360° on color wheel)
        .SetLoops(-1, LoopType.Restart);
    }

    public void StopAnimation()
    {
        colorTween?.Kill();
        colorTween = null;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
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