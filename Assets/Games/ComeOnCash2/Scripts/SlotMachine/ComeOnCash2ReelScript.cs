using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class ComeOnCash2ReelScript : MonoBehaviour
{
    #region Variables

    // Machine Variables
    [Header("Slot Machine")]
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    public List<ComeOnCash2SlotScript> slots;
    private RectTransform _rectTransform;
    private ComeOnCash2SpinSettings _spinSettings;
    private Vector2 _targetPos;

    // State Variables
    private bool _inSpin;
    private bool _inClamp;
    private bool _increaseSpeed;
    private bool _clampedDown;
    private bool _forceStop;
    private bool allowSymbolChanges = true;
    private bool _nextIsRealSymbol = true;
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
    public delegate void ComeOnCash2ReelEvents(int index);
    public static event ComeOnCash2ReelEvents OnSpinStart;
    public static event ComeOnCash2ReelEvents OnSpinComplete;

    // Result Data
    private List<SymbolData> finalResultSymbols;

    public enum ReelDirection
    {
        Down = -1,
        Up = 1
    }

    [SerializeField] private ReelDirection reelDirection = ReelDirection.Down;
    private int Direction => (int)reelDirection;


    #endregion

    #region Unity Methods

    private void Start()
    {
        if (ComeOnCash2SlotMachine.Instance != null)
            ComeOnCash2SlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (ComeOnCash2SlotMachine.Instance != null)
            ComeOnCash2SlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Initialization

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    public void Initialize(int index)
    {
        _rectTransform = GetComponent<RectTransform>();

        this._forceStop = false;
        this._inSpin = false;
        this._index = index;


        if (slots == null || slots.Count != 4)
        {
            slots = new List<ComeOnCash2SlotScript>(GetComponentsInChildren<ComeOnCash2SlotScript>());

        }

        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, i);
        }
        _spinSettings = ComeOnCash2SlotMachine.Instance.settings.spinSettings;
    }

    #endregion

    #region Reel Settings

    public void ResetShape()
    {
        ComeOnCash2SlotMachine.Instance.isResultReceived = false;
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
        if (reelIndex >= ComeOnCash2SlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = ComeOnCash2SlotMachine.Instance.spinSymbolMatrix[reelIndex];

        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 1; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = ComeOnCash2SlotMachine.GetResourceById(symbolData.id);
            int row = rowIndex;

            if (res.HasValue)
            {
                //DoubleJackpotBullseyeSlotMachine.Instance.isResultReceived = true;

                if (_clampedDown)
                {
                    row -= 1;
                }

                var slot = slots[row + 1]; // Make sure slots[1], [2], [3] are the visible ones

                slot.SetType(res.Value);
            }
            else
            {
                Debug.LogWarning($"⚠️ No slot resource found for ID: {symbolData.id}");
            }
        }

        //if (!ComeOnCash2AutoSpinController.isAutoSpinning && !ComeOnCash2SlotMachine.Instance.isFreeGameReady)
        //{
        //    ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
        //}

        StopAllCoroutines();
    }

    public ComeOnCash2SlotType GetSlotType(int index)
    {
        return slots[index + 1].type;
    }

    #endregion

    #region Spin & Stop

    public void Spin(float delay, float acceleration, float speed)
    {
        slots[0].SetType(ComeOnCash2SlotMachine.CachedRealSymbols[Random.Range(0, ComeOnCash2SlotMachine.CachedRealSymbols.Count)]);

        canStopReel = false;
        resultApplied = false;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        if (_spinSettings.endSpin == ComeOnCash2SpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        _acceleration = acceleration <= 0 ? ComeOnCash2GameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? ComeOnCash2GameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
        _yOffset = 0f;
        _increaseSpeed = true;
        _inClamp = false;
        _inSpin = true;

        _forceStop = false;

        OnSpinStart?.Invoke(this._index);
    }

    public void Stop()
    {
        allowSymbolChanges = false;
        _inClamp = true;
        _inSpin = false;

        var xPos = _rectTransform.anchoredPosition.x;
        var topPos = new Vector2(xPos, _spinSettings.topBoundary);
        var bottomPos = new Vector2(xPos, _spinSettings.bottomBoundary);

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

            //var topSlot = CashMachineSlotMachine.GetResourceById("Empty");
            //slots[0].SetType(topSlot.Value);
            //slots[slots.Count - 1].SetType(topSlot.Value);

            Debug.Log("Reel " + this._index + " Clamped Down");
        }
        ComeOnCash2UIManager.Instance.PlaySound("ReelStop");
        OnSpinComplete?.Invoke(this._index);
    }

    private ComeOnCash2SlotResource GetNextGeneratedSymbol()
    {
        _nextIsRealSymbol = false;
        var real = ComeOnCash2SlotMachine.CachedRealSymbols;
        return real[Random.Range(0, real.Count)];
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

        //if (_increaseSpeed || !DoubleJackpotBullseyeSlotMachine.Instance.isResultReceived)
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

        _rectTransform.Translate(Vector3.up * Direction * (_currentSpeed * Time.deltaTime));
        var currentPos = _rectTransform.anchoredPosition;
        if (Direction == -1 && currentPos.y <= _spinSettings.bottomBoundary)
        {
            WrapToTop();
        }
        else if (Direction == 1 && currentPos.y >= _spinSettings.topBoundary)
        {
            WrapToBottom();
        }
    }

    private void WrapToTop()
    {
        var currentPos = _rectTransform.anchoredPosition;
        _yOffset = _spinSettings.bottomBoundary - currentPos.y;
        _rectTransform.anchoredPosition =
            new Vector2(currentPos.x, _spinSettings.topBoundary + _yOffset);

        ShiftSymbolsForward();
    }

    private void WrapToBottom()
    {
        var currentPos = _rectTransform.anchoredPosition;
        _yOffset = currentPos.y - _spinSettings.topBoundary;
        _rectTransform.anchoredPosition =
            new Vector2(currentPos.x, _spinSettings.bottomBoundary - _yOffset);

        ShiftSymbolsBackward();
    }

    private void ShiftSymbolsForward()
    {
        for (int i = slots.Count - 1; i > 0; i--)
            slots[i].SetType(slots[i - 1].currentResource);

        slots[0].SetType(GetNextGeneratedSymbol());
    }

    private void ShiftSymbolsBackward()
    {
        for (int i = 0; i < slots.Count - 1; i++)
            slots[i].SetType(slots[i + 1].currentResource);

        slots[slots.Count - 1].SetType(GetNextGeneratedSymbol());
    }

    public void ReverseDirection(bool moveUp)
    {
        reelDirection = moveUp ? ReelDirection.Up : ReelDirection.Down;
    }
    #endregion
}
