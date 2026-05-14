using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class TheGreenMachineDeluxeSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
    [HideInInspector] public TheGreenMachineDeluxeSlotType type;
    [HideInInspector] public TheGreenMachineDeluxeSlotResource currentResource;
    public TheGreenMachineDeluxeSlotCategory category;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private TheGreenMachineDeluxeReelScript _parent;
    private RectTransform _rectTransform;

    [Header("Slot Resources")]
    [SerializeField] private GameObject[] slots;
    [SerializeField] private Image frame;
    [SerializeField] private Image cashIcon;
    [SerializeField] private Image spinIcon;
    [SerializeField] private Image spinTextIcon;
    [SerializeField] private Image jackpotIcon;
    [SerializeField] private Image jackpotTextIcon;

    [Header("Animation Settings")]
    public float scaleAmount = 1.2f;
    public float animationDuration = 0.5f;
    public float delayBetween = 0.2f;
    public float freezeDuration = 0.2f;
    [SerializeField] private UIShiny uiShiny;
    private Tween cashTween;
    private Tween freeSpinTween;
    private Tween freeSpinTextTween;
    private Tween jackpotTween;
    private Tween jackpotTextTween;

    private float emptyWeight = 50f;
    private float cashWeight = 30f;
    private float freeSpinWeight = 15f;
    private float jackpotWeight = 5f;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (TheGreenMachineDeluxeSlotMachine.Instance != null)
            TheGreenMachineDeluxeSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (TheGreenMachineDeluxeSlotMachine.Instance != null)
            TheGreenMachineDeluxeSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void Initialize(TheGreenMachineDeluxeReelScript parentReel, int index)
    {
        this.index = index;
        this._parent = parentReel;
        _rectTransform = GetComponent<RectTransform>();

        // Weights
        emptyWeight = TheGreenMachineDeluxeSlotMachine.Instance.emptyWeight;
        cashWeight = TheGreenMachineDeluxeSlotMachine.Instance.cashWeight;
        freeSpinWeight = TheGreenMachineDeluxeSlotMachine.Instance.freeSpinWeight;
        jackpotWeight = TheGreenMachineDeluxeSlotMachine.Instance.jackpotWeight;

        SetType(TheGreenMachineDeluxeSlotMachine.Instance.settings.resourcesList[0]);
    }

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }
    
    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void GetRandom()
    {
        float total = emptyWeight + cashWeight + freeSpinWeight + jackpotWeight;
        float roll = Random.Range(0f, total);

        TheGreenMachineDeluxeSlotResource selected;

        if (roll < emptyWeight && TheGreenMachineDeluxeSlotMachine.Instance.emptyList.Count > 0)
            selected = TheGreenMachineDeluxeSlotMachine.Instance.emptyList[Random.Range(0, TheGreenMachineDeluxeSlotMachine.Instance.emptyList.Count)];
        else if ((roll -= emptyWeight) < cashWeight && TheGreenMachineDeluxeSlotMachine.Instance.cashList.Count > 0)
            selected = TheGreenMachineDeluxeSlotMachine.Instance.cashList[Random.Range(0, TheGreenMachineDeluxeSlotMachine.Instance.cashList.Count)];
        else if ((roll -= cashWeight) < freeSpinWeight && TheGreenMachineDeluxeSlotMachine.Instance.freeSpinList.Count > 0)
            selected = TheGreenMachineDeluxeSlotMachine.Instance.freeSpinList[Random.Range(0, TheGreenMachineDeluxeSlotMachine.Instance.freeSpinList.Count)];
        else if (TheGreenMachineDeluxeSlotMachine.Instance.jackpotList.Count > 0)
            selected = TheGreenMachineDeluxeSlotMachine.Instance.jackpotList[Random.Range(0, TheGreenMachineDeluxeSlotMachine.Instance.jackpotList.Count)];
        else
            selected = TheGreenMachineDeluxeSlotMachine.Instance.settings.resourcesList[Random.Range(0, TheGreenMachineDeluxeSlotMachine.Instance.settings.resourcesList.Count)]; // fallback

        SetType(selected);
    }

    public void SetType(TheGreenMachineDeluxeSlotResource newType)
    {
        this.currentResource = newType;
        this.type = newType.type;
        this.category = newType.category;
        this.frame.sprite = newType.frameImage;

        if (category == TheGreenMachineDeluxeSlotCategory.Empty)
        {
            HideAllIcons();
        }
        else if (category == TheGreenMachineDeluxeSlotCategory.Cash)
        {
            this.cashIcon.sprite = newType.winImage;
            SetActiveIcon(0);
        }
        else if (category == TheGreenMachineDeluxeSlotCategory.FreeSpin)
        {
            this.spinIcon.sprite = newType.winImage;
            this.spinTextIcon.sprite = newType.textImage;
            SetActiveIcon(1);
        }
        else if (category == TheGreenMachineDeluxeSlotCategory.Jackpot)
        {
            this.jackpotIcon.sprite = newType.winImage;
            this.jackpotTextIcon.sprite = newType.textImage;
            SetActiveIcon(2);
        }
    }

    private void SetActiveIcon(int index)
    {
        HideAllIcons();
        slots[index].SetActive(true);
    }

    private void HideAllIcons()
    {
        foreach (GameObject slot in slots)
        {
            slot.SetActive(false);
        }
    }

    #endregion

    #region Slot Animation

    public void StartAnimation()
    {
        StopAnimation(); // reset any existing tweens

        //Debug.Log($"\nReel: {gameObject.transform.parent.name}\nSlot: {gameObject.name}\nSlot Category: {category}");

        switch (category)
        {
            case TheGreenMachineDeluxeSlotCategory.Cash:
                cashTween = CreateTweenWithPause(cashIcon, delayBetween, scaleAmount * 1.25f, animationDuration, freezeDuration);
                break;
            case TheGreenMachineDeluxeSlotCategory.FreeSpin:
                freeSpinTextTween = CreateTweenWithPause(spinTextIcon, delayBetween, scaleAmount, animationDuration, freezeDuration);
                freeSpinTween = CreateTweenWithPause(spinIcon, delayBetween, scaleAmount * 1.25f, animationDuration, freezeDuration);
                break;
            case TheGreenMachineDeluxeSlotCategory.Jackpot:
                jackpotTextTween = CreateTweenWithPause(jackpotTextIcon, delayBetween, scaleAmount, animationDuration, freezeDuration);
                jackpotTween = CreateTweenWithPause(jackpotIcon, delayBetween, scaleAmount * 1.3f, animationDuration, freezeDuration);
                break;
            default:
                return;
        }
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        if (uiShiny != null)
        {
            uiShiny.Stop();
            uiShiny.enabled = false;
        }

        cashTween?.Kill(); cashTween = null;
        freeSpinTween?.Kill(); freeSpinTween = null;
        freeSpinTextTween?.Kill(); freeSpinTextTween = null;
        jackpotTween?.Kill(); jackpotTween = null;
        jackpotTextTween?.Kill(); jackpotTextTween = null;

        if (cashIcon) cashIcon.rectTransform.localScale = Vector3.one;
        if (spinIcon) spinIcon.rectTransform.localScale = Vector3.one;
        if (spinTextIcon) spinTextIcon.rectTransform.localScale = Vector3.one;
        if (jackpotIcon) jackpotIcon.rectTransform.localScale = Vector3.one;
        if (jackpotTextIcon) jackpotTextIcon.rectTransform.localScale = Vector3.one;
    }

    private Tween CreateTweenWithPause(Image target, float delay, float scale, float duration, float pause)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        var rect = target.rectTransform;
        
        uiShiny = target.gameObject.GetComponent<UIShiny>();

        if (uiShiny != null)
        {
            //Debug.Log($"\nReel: {gameObject.transform.parent.name}\nSlot: {gameObject.name}\nSlot Category: {category}");
            uiShiny.enabled = false;
            uiShiny.Stop();
        }

        Sequence sequence = DOTween.Sequence();

        // Start after the initial delay
        sequence.AppendInterval(delay);

        // Scale up 
        sequence.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));

        // Play shiny effect after scaling up
        sequence.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.enabled = true;
                uiShiny.Play();
            }
        });

        // Pause at peak scale
        sequence.AppendInterval(pause);

        // Disable the shiny effect before scaling back down
        sequence.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.Stop();
                uiShiny.enabled= false;
            }
        });

        // Scale back to original size
        sequence.Append(rect.DOScale(1f, duration).SetEase(Ease.InSine));

        // Pause at reset
        sequence.AppendInterval(pause * 0.5f);

        // Add looping to the sequence (Set it to loop infinitely)
        sequence.SetLoops(-1, LoopType.Restart); // Loops forever with Yoyo (scales up and down repeatedly)

        return sequence;
    }

    #endregion
}
