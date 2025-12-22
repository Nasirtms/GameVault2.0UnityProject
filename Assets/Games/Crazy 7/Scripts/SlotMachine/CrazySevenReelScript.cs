using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CrazySevenReelScript : MonoBehaviour
{
    #region Variables
    //Events
    public delegate void CrazySevenReelEvents(int index);
    public static event CrazySevenReelEvents OnSpinStart;
    public static event CrazySevenReelEvents OnSpinComplete;

    // Machine Variables
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    [SerializeField] private List<CrazySevenSlotType> slotsType = new List<CrazySevenSlotType>(); 
    public List<CrazySevenSlotScript> slots;
    private RectTransform _rectTransform;
    private CrazySevenSpinSettings _spinSettings;

    //State Variables
    private bool _inSpin;
    private bool _inClamp;
    private float _yOffset;
    private bool _forceStop;
    private bool allowSymbolChanges = true;
    private Vector2 _targetPos;
    private bool resultApplied;
    private bool _clampedDown;
    public bool canStopReel = false;

    //Spin Variables
    [SerializeField] private int _index;
    private float _currentSpeed;
    private bool _increaseSpeed;
    private float _delayAmount;
    private float _timeCounter;
    private float _acceleration;

    //Result Data
    private List<SymbolData> finalResultSymbols;
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (CrazySevenSlotMachine.Instance != null)
            CrazySevenSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (CrazySevenSlotMachine.Instance != null)
            CrazySevenSlotMachine.Instance.StopReelProcess -= HandleStart;
    }
    #endregion

    #region Initialization
    private void HandleStart()
    {
        StopAllCoroutines();
    }
    
    public void Initialize(int index)
    {
        //CrazySevenSlotMachine.Instance.StopReelProcess += HandleStart;
        _rectTransform = GetComponent<RectTransform>();
        this._forceStop = false;
        this._inSpin = false;
        this._index = index;

        if (slots == null || slots.Count != 4)
        {
            slots = new List<CrazySevenSlotScript>(GetComponentsInChildren<CrazySevenSlotScript>());
        }

        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, reelIndex, i);
        }
        //AssignUniqueRandomVisualsToReelSlots();

        _spinSettings = CrazySevenSlotMachine.Instance.settings.spinSettings;
    }
    #endregion

    #region Reel Settings
    public void ResetShape()
    {
        CrazySevenSlotMachine.Instance.isResultReceived = false;
        foreach (var slot in slots)
        {
            slot.isResultSet = false;
            slot.ToggleSlotBorder(false);
        }
        CrazySevenSlotMachine.Instance.BorderSlots.Clear();
    }
    public void UpdateVerticalLayout(float spacing, int padding)
    {
        verticalLayout.padding.top = padding;
        verticalLayout.spacing = spacing;
    }

    public void UpdateSlotScale(float scale)
    {
        foreach (var slot in slots)
        {
            slot.UpdateScale(scale);
        }
    }
    #endregion

    #region Spin Result
    public int reelIndex;
    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= CrazySevenSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            Debug.LogError($"❌ No spin data for reel {reelIndex}!");
            return;
        }
        
        var symbols = CrazySevenSlotMachine.Instance.spinSymbolMatrix[reelIndex];
        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = CrazySevenSlotMachine.GetResourceById(symbolData.id);
            int row = rowIndex;

            if (res.HasValue)
            {
                //CrazySevenSlotMachine.Instance.isResultReceived = true;

                if (_clampedDown)
                {
                    row -= 1;
                }

                var slot = slots[row + 1];
                slot.SetType(res.Value);

                slot.ReelIndex = reelIndex;
                slot.RowIndex = rowIndex;          

                // 6️⃣ Add to BorderSlots if it has a border for win display
                if (symbolData.showBorder && !CrazySevenSlotMachine.Instance.BorderSlots.Contains(slot))
                {
                    CrazySevenSlotMachine.Instance.BorderSlots.Add(slot);
                    slot.paylineNumberList.Clear();
                    slot.paylineNumberList.AddRange(symbolData.paylineNumbers);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ No slot resource found for ID: {symbolData.id}");
            }
        }
        StopAllCoroutines();
    }

    public CrazySevenSlotType GetSlotType(int index)
    {
        return slots[index + 1].type;
    }
    public RectTransform GetSlotTransform(int index)
    {
        return slots[index + 1].gameObject.GetComponent<RectTransform>();
    }
    #endregion

    #region Spin & Stop
    public void Spin(float delay, float acceleration, float speed)
    {
        canStopReel = false;
        resultApplied = false;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        if (_spinSettings.endSpin == CrazySevenSpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        _acceleration = acceleration <= 0 ? CrazySevenGameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? CrazySevenGameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
        _yOffset = 0f;
        _increaseSpeed = true;
        _inClamp = false;
        _inSpin = true;
        _forceStop = false;
        OnSpinStart?.Invoke(this._index);
    }
    private void OnClampComplete()
    {
        if (_clampedDown)
        {
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _spinSettings.topBoundary);

            for (var i = slots.Count - 1; i > 0; i--)
            {
                var res = slots[i - 1].currentResource;
                slots[i].SetType(res);
            }
        }
        OnSpinComplete?.Invoke(this._index);
    }

    public void Stop()
    {
        allowSymbolChanges = false;
        _inClamp = true;
        _inSpin = false;

        var xPos = _rectTransform.anchoredPosition.x;
        var topPos = new Vector2(xPos, _spinSettings.topBoundary);
        var bottomPos = new Vector2(xPos, _spinSettings.bottomBoundary);
        var disTop = Vector3.Distance(_rectTransform.anchoredPosition, topPos);
        var disBot = Vector3.Distance(_rectTransform.anchoredPosition, bottomPos);

        if (Vector3.Distance(_rectTransform.anchoredPosition, topPos) < Vector3.Distance(_rectTransform.anchoredPosition, bottomPos))
        {
            //clamp to top
            _targetPos = topPos;
            _clampedDown = false;
        }
        else
        {
            //clamp to bottom
            _targetPos = bottomPos;
            _clampedDown = true;
        }
    }
    public void ForceStop()
    {
        _forceStop = true;
    }

    public bool IsClamped()
    {
        return !_inSpin && !_inClamp;
    }

    private void Update()
    {
        if (_inClamp)
        {
            if (!resultApplied)
            {
                ApplyFinalResult(this._index);
            }

            if (_currentSpeed > (_spinSettings.speedRange.x / 2)) _currentSpeed -= _acceleration;
            _rectTransform.anchoredPosition = Vector3.LerpUnclamped(_rectTransform.anchoredPosition, _targetPos,
                _currentSpeed * Time.deltaTime);

            if (Vector3.Distance(_rectTransform.anchoredPosition, _targetPos) < _spinSettings.minClamp)
            {
                _rectTransform.anchoredPosition = _targetPos;
                _inClamp = false;
                OnClampComplete();
            }
        }

        if (!_inSpin) return;

        //if (_increaseSpeed || !CrazySevenSlotMachine.Instance.isResultReceived)
        if (!canStopReel)
        {
            if (_currentSpeed < _spinSettings.speedRange.y)
            {
                _currentSpeed += _acceleration;
            }
            else
            {
                if (_timeCounter >= _delayAmount)
                {
                    _increaseSpeed = false;
                }
                else
                {
                    _timeCounter += Time.deltaTime;
                }
            }
        }
        else
        {
            if (_currentSpeed > _spinSettings.speedRange.x)
            {
                _currentSpeed -= _acceleration;
            }
            else
            {
                var distance = (Vector3.Distance(_rectTransform.anchoredPosition, _targetPos) / 50) / 2;
                if ((_currentSpeed - distance) > 1) _currentSpeed = _currentSpeed - distance;
                Stop();
            }
        }

        if (_forceStop) Stop();

        _rectTransform.Translate(Vector3.down * (_currentSpeed * Time.deltaTime));
        var currentPos = _rectTransform.anchoredPosition;
        if (currentPos.y <= _spinSettings.bottomBoundary)
        {
            _yOffset = _spinSettings.bottomBoundary - currentPos.y;
            _rectTransform.anchoredPosition = new Vector2(currentPos.x, _spinSettings.topBoundary + _yOffset);

            if (_inSpin && allowSymbolChanges && finalResultSymbols == null)
            {
                for (var i = slots.Count - 1; i > 0; i--)
                {
                    var res = slots[i - 1].currentResource;
                    slots[i].SetType(res);
                    //if (!slots[i].isResultSet)
                    //{
                    //    if (!CrazySevenSlotMachine.Instance.isStopBtnPressed || !CrazySevenSlotMachine.Instance.isResultReceived)
                    //    {
                    //        AssignUniqueRandomVisualsToReelSlots();


                    //    }
                    //}
                }
                slots[0].GetRandom();
            }
        }
    }
    #endregion
}
