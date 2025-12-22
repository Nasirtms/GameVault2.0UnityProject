using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CrazySevenSlotScript : MonoBehaviour
{
    #region Variables
    public CrazySevenSlotType type;
    public int index;
    public bool isResultSet = false;
    public int paylineNumber;
    public int ReelIndex;
    public int RowIndex;
    public GameObject border;
    public List<int> paylineNumberList = new List<int>();
    

    private Image _background;
    private RectTransform _rectTransform;
    private CrazySevenReelScript _parent;

    [SerializeField] private Image icon; 
    public CrazySevenSlotResource currentResource;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        if (CrazySevenSlotMachine.Instance != null)
            CrazySevenSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDisable()
    {
        if (CrazySevenSlotMachine.Instance != null)
            CrazySevenSlotMachine.Instance.StopReelProcess -= HandleStart;
    }
    #endregion

    #region Slot Settings
    private void HandleStart()
    {
        StopAllCoroutines();
    }

    public void SetBorderVisible(bool visible)
    {
        if (border != null)
            border.SetActive(visible);
    }
    public void Initialize(CrazySevenReelScript parentReel, int reelIndex, int rowIndex)
    {
        this._parent = parentReel;
        this.ReelIndex = reelIndex;
        this.RowIndex = rowIndex;
        this.index = rowIndex;
        icon = transform.GetChild(0).GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        ToggleSlotBorder(false);
        GetRandom();
    }
    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }
    #endregion


    public void SetVisibility(bool status)
    {
        gameObject.SetActive(status);
    }

    public void SetType(CrazySevenSlotResource newType)
    {
            this.currentResource = newType;
            this.type = newType.type;

            if (icon != null)
            {
                icon.sprite = newType.background;
                icon.SetNativeSize();
                icon.enabled = true;
            }
    }

    public void GetRandom()
    {
        var random = CrazySevenSlotMachine.Instance.settings.resourcesList[Random.Range(0, CrazySevenSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
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
}