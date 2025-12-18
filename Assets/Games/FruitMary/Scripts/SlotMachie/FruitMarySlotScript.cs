using DG.Tweening;
using Sequence = DG.Tweening.Sequence;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FruitMarySlotScript : MonoBehaviour
{
    public FruitMarySlotType type;
    public int index;
    public bool isResultSet = false;
    public int paylineNumber;
    public int ReelIndex;
    public int RowIndex;
    public GameObject border; 
    public List<int> paylineNumberList = new List<int>();

    private RectTransform _rectTransform;
    private FruitMaryReelScript _parent;
    public Tween animTween;

    [SerializeField] private Image icon;
    public FruitMarySlotResource currentResource;
    [SerializeField] private float scaleAmount = 1.1f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float animationPause = 0.75f;

    private void OnEnable()
    {
        if (FruitMarySlotMachine.Instance != null)
            FruitMarySlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDisable()
    {
        if (FruitMarySlotMachine.Instance != null)
            FruitMarySlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }
    public void SetBorderVisible(bool visible)
    {
        if (border != null)
            border.SetActive(visible);
    }
    public void Initialize(FruitMaryReelScript parentReel, int reelIndex, int rowIndex)
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

    public void SetType(FruitMarySlotResource newType)
    {
        this.currentResource = newType;
        this.type = newType.type;

        icon.sprite = newType.background;
    }
    public void ShowRandomVisual()
    {
        FruitMarySlotResource random;
        string parentName = gameObject.transform.parent.name;
        do
        {
            random = GetRandomSlot();
        }
        while (
        (parentName == "Reels0" || parentName == "Reels4") &&
        (random.type == FruitMarySlotType.SCATTER || random.type == FruitMarySlotType.WILD)
        );

        SetType(random);
    }
    FruitMarySlotResource GetRandomSlot()
    {

        var random = FruitMarySlotMachine.Instance.settings.resourcesList[
                Random.Range(0, FruitMarySlotMachine.Instance.settings.resourcesList.Count)];

        return random;
    }
    public void AnimateSymbol(float duration = 0.25f)
    {
        if (_rectTransform == null) return;

        _rectTransform.localScale = Vector3.zero;
    }

    public void ToggleSlotBorder(bool visible)
    {
        if (border != null)
        {
            border.SetActive(visible);
        }
        else
        {
            Debug.LogWarning($"❌ Slot {gameObject.name} missing border reference!");
        }
    }


    public void ShowUniqueRandomVisual(HashSet<FruitMarySlotType> excludedTypes)
    {
        FruitMarySlotResource random;
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
                 (random.type == FruitMarySlotType.SCATTER || random.type == FruitMarySlotType.WILD))
            )
        );

        excludedTypes.Add(random.type);
        SetType(random);
    }

    public void PlayAnimation()
    {
        if (icon == null) return;

        var rt = icon.rectTransform;
        rt.DOKill(false);
        animTween?.Kill();
        rt.localScale = Vector3.one;

        animTween = DOTween.Sequence()
            .Append(rt.DOScale(scaleAmount, animationDuration).SetEase(Ease.OutSine))
            .AppendInterval(animationPause)
            .Append(rt.DOScale(1f, animationDuration).SetEase(Ease.OutSine))
            .AppendInterval(animationPause)
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopAnimation()
    {
        if (animTween != null && animTween.IsActive())
        {
            animTween.Kill();
            animTween = null;
        }

        if (icon != null)
        {
            var rt = icon.rectTransform;
            rt.DOKill(false);       
            rt.localScale = Vector3.one; 
        }
    }
}