using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class CashMachineReelScript : MonoBehaviour
{
    #region Variables

    public enum ReelDirection
    {
        Up = 1,
        Down = -1
    }

    [SerializeField] private ReelDirection reelDirection = ReelDirection.Down;
    private int direction => (int)reelDirection;

    // Machine Variables
    [Header("Slot Machine")]
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    public List<CashMachineSlotScript> slots;
    private RectTransform _rectTransform;
    private CashMachineSpinSettings _spinSettings;
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
    public delegate void CashMachineReelEvents(int index);
    public static event CashMachineReelEvents OnSpinStart;
    public static event CashMachineReelEvents OnSpinComplete;

    // Result Data
    private List<SymbolData> finalResultSymbols;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (CashMachineSlotMachine.Instance != null)
            CashMachineSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (CashMachineSlotMachine.Instance != null)
            CashMachineSlotMachine.Instance.StopReelProcess -= HandleStart;
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
            slots = new List<CashMachineSlotScript>(GetComponentsInChildren<CashMachineSlotScript>());

        }

        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, i);
        }
        _spinSettings = CashMachineSlotMachine.Instance.settings.spinSettings;
    }

    #endregion

    #region Reel Settings

    public void ResetShape()
    {
        CashMachineSlotMachine.Instance.isResultReceived = false;
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
        if (reelIndex >= CashMachineSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = CashMachineSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 1; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = CashMachineSlotMachine.GetResourceById(symbolData.id);
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
        }

        //if (!CashMachineAutoSpinController.isAutoSpinning && !CashMachineSlotMachine.Instance.isFreeGameReady)
        //{
        //    CashMachineUIManager.Instance.UpdateButtons("Stop");
        //}

        StopAllCoroutines();
    }

    public CashMachineSlotType GetSlotType(int index)
    {
        return slots[index + 1].type;
    }

    #endregion

    #region Spin & Stop

    public void Spin(float delay, float acceleration, float speed)
    {
        slots[0].SetType(CashMachineSlotMachine.CachedRealSymbols[Random.Range(0, CashMachineSlotMachine.CachedRealSymbols.Count)]);

        canStopReel = false;
        resultApplied = false;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        if (_spinSettings.endSpin == CashMachineSpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        _acceleration = acceleration <= 0 ? CashMachineGameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? CashMachineGameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
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

        }
        CashMachineUIManager.Instance.PlaySound("ReelStop");
        OnSpinComplete?.Invoke(this._index);
    }

    private CashMachineSlotResource GetNextGeneratedSymbol()
    {
        _nextIsRealSymbol = false;
        var real = CashMachineSlotMachine.CachedRealSymbols;
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

        _rectTransform.Translate(Vector3.up * direction * (_currentSpeed * Time.deltaTime));
        var currentPos = _rectTransform.anchoredPosition;
        if (direction == -1 && currentPos.y <= _spinSettings.bottomBoundary)
        {
            WrapToTop();
        }
        else if (direction == 1 && currentPos.y >= _spinSettings.topBoundary)
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
