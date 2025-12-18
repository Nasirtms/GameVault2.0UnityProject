using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class TenTimesWinsReelScript : MonoBehaviour
{
    #region Variables

    // Machine Variables
    [Header("Slot Machine")]
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    public List<TenTimesWinsSlotScript> slots;
    private RectTransform _rectTransform;
    private TenTimesWinsSpinSettings _spinSettings;
    private Vector2 _targetPos;
    
    // State Variables
    private bool _inSpin;
    private bool _inClamp;
    private bool _increaseSpeed;
    private bool _clampedDown;
    private bool _forceStop;
    private bool allowSymbolChanges = true;
    public bool canStopReel = false;
    private bool resultApplied;

    // Spin Variables
    private int _index;
    private float _yOffset;
    private float _currentSpeed;
    private float _delayAmount;
    private float _timeCounter;
    private float _acceleration;

    // Events
    public delegate void TenTimesWinsReelEvents(int index);
    //public delegate void TenTimesWinsAnimationEvents();
    public static event TenTimesWinsReelEvents OnSpinStart;
    public static event TenTimesWinsReelEvents OnSpinComplete;
    //public static event TenTimesWinsAnimationEvents PlaySlotAnimations;

    // Result Data
    private List<SymbolData> finalResultSymbols;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (TenTimesWinsSlotMachine.Instance != null)
            TenTimesWinsSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (TenTimesWinsSlotMachine.Instance != null)
            TenTimesWinsSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Initialization

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    public void Initialize(int index)
    {
        TenTimesWinsSlotMachine.Instance.StopReelProcess += HandleStart;
        _rectTransform = GetComponent<RectTransform>();

        this._forceStop = false;
        this._inSpin = false;
        this._index = index;

        if (slots == null || slots.Count != 4)
        {
            slots = new List<TenTimesWinsSlotScript>(GetComponentsInChildren<TenTimesWinsSlotScript>());
        }

        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, i);
        }

        AssignUniqueRandomVisualsToReelSlots();

        _spinSettings = TenTimesWinsSlotMachine.Instance.settings.spinSettings;
    }
    
    private void AssignUniqueRandomVisualsToReelSlots()
    {
        Dictionary<string, HashSet<TenTimesWinsSlotType>> reelSlotTypes = new();

        foreach (var slot in slots)
        {
            string reelName = slot.transform.parent.name;

            if (!reelSlotTypes.ContainsKey(reelName))
                reelSlotTypes[reelName] = new HashSet<TenTimesWinsSlotType>();

            // Assign visual with uniqueness check
            slot.ShowUniqueRandomVisual(reelSlotTypes[reelName]);
        }
    }

    #endregion

    #region Reel Settings

    public void ResetShape()
    {
        TenTimesWinsSlotMachine.Instance.isResultReceived = false;
        foreach (var slot in slots)
        {
            slot.isResultSet = false;
        }
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

    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= TenTimesWinsSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            Debug.LogError($"❌ No spin data for reel {reelIndex}!");
            return;
        }

        var symbols = TenTimesWinsSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = TenTimesWinsSlotMachine.GetResourceById(symbolData.id);
            int row = rowIndex;

            if (res.HasValue)
            {
                //TenTimesWinsSlotMachine.Instance.isResultReceived = true;

                if (_clampedDown)
                {
                    row -= 1;

                    Debug.Log("Slot Clamped on Reel: " + this._index + " row: " + row);
                }

                var slot = slots[row + 1]; // Make sure slots[1], [2], [3] are the visible ones

                slot.SetType(res.Value);

                //slot.reelIndex = reelIndex;
                //slot.rowIndex = rowIndex;

                //if (symbolData.showBorder && !CleopatraSlotMachine.Instance.BorderSlots.Contains(slot))
                //{
                //    CleopatraSlotMachine.Instance.BorderSlots.Add(slot);

                //    slot.paylineNumberList.Clear();
                //    slot.paylineNumberList.AddRange(symbolData.paylineNumbers);
                //}
            }
            else
            {
                Debug.LogWarning($"⚠️ No slot resource found for ID: {symbolData.id}");
            }
        }

        StopAllCoroutines();
        // Apply hidden clone
        //slots[0].SetType(slots[0].currentResource);
    }

    public TenTimesWinsSlotType GetSlotType(int index)
    {
        return slots[index + 1].type;
    }

    #endregion

    #region Spin & Stop

    public void Spin(float delay, float acceleration, float speed)
    {
        canStopReel = false;
        resultApplied = false;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        if (_spinSettings.endSpin == TenTimesWinsSpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        _acceleration = acceleration <= 0 ? TenTimesWinsGameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? TenTimesWinsGameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
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

            //shift types
            for (var i = slots.Count - 1; i > 0; i--)
            {
                var res = slots[i - 1].currentResource;
                slots[i].SetType(res);
            }

            Debug.Log("Reel " + this._index + " Clamped");
        }
        TenTimesWinsUIManager.Instance.PlaySound("ReelStop");
        OnSpinComplete?.Invoke(this._index);
        //PlaySlotAnimations?.Invoke();

    }

    public void Stop()
    {
        allowSymbolChanges = false;
        _inClamp = true;
        _inSpin = false;

        var xPos = _rectTransform.anchoredPosition.x;
        var topPos = Vector2.zero;
        var bottomPos = Vector2.zero;

            topPos = new Vector2(xPos, _spinSettings.topBoundary);
            bottomPos = new Vector2(xPos, _spinSettings.bottomBoundary);

            var disTop = Vector3.Distance(_rectTransform.anchoredPosition, topPos);
            var disBot = Vector3.Distance(_rectTransform.anchoredPosition, bottomPos);

            if (Vector3.Distance(_rectTransform.anchoredPosition, topPos) <
                Vector3.Distance(_rectTransform.anchoredPosition, bottomPos))
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

        //if (_increaseSpeed || !TenTimesWinsSlotMachine.Instance.isResultReceived)
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
            //reset pos
            _yOffset = _spinSettings.bottomBoundary - currentPos.y;

            _rectTransform.anchoredPosition = new Vector2(currentPos.x, _spinSettings.topBoundary + _yOffset);

            if (_inSpin && allowSymbolChanges && finalResultSymbols == null)
            {
                //shift types
                for (var i = slots.Count - 1; i > 0; i--)
                {
                    var res = slots[i - 1].currentResource;
                    slots[i].SetType(res);
                }

                //generate new
                slots[0].GetRandom();
            }
        }
    }

    #endregion
}
