using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(VerticalLayoutGroup))]

public class RedHotTrippleReelScript : MonoBehaviour
{
    #region Variables

    // Machine Variables
    [Header("Slot Machine")]
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    public List<RedHotTrippleSlotScript> slots;
    private RectTransform _rectTransform;
    private RedHotTrippleSpinSettings _spinSettings;
    private Vector2 _targetPos;
    private RedHotTrippleSpinDirection _spinDirection;

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
    // Result Data
    private List<SymbolData> finalResultSymbols;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (RedHotTrippleSlotMachine.Instance != null)
            RedHotTrippleSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (RedHotTrippleSlotMachine.Instance != null)
            RedHotTrippleSlotMachine.Instance.StopReelProcess -= HandleStart;
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

        if (slots == null || slots.Count != 5)
        {
            slots = new List<RedHotTrippleSlotScript>(GetComponentsInChildren<RedHotTrippleSlotScript>());

        }

        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, i);
        }

        _spinSettings = RedHotTrippleSlotMachine.Instance.settings.spinSettings;
    }

    #endregion

    #region Reel Settings

    public void ResetShape()
    {
        RedHotTrippleSlotMachine.Instance.isResultReceived = false;
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
        if (reelIndex >= RedHotTrippleSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = RedHotTrippleSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = RedHotTrippleSlotMachine.GetResourceById(symbolData.id);
            int row = rowIndex;

            if (res.HasValue)
            {
                if (_clampedDown)
                {
                    if (_spinDirection == RedHotTrippleSpinDirection.Downwards)
                    {
                        row -= 1;
                    }
                    else
                    {
                        row += 1;
                    }
                }

                var slot = slots[row + 2]; // Make sure slots[1], [2], [3] are the visible ones

                slot.SetType(res.Value);
            }
            //ApplyReelOffset();
        }

        var extraSlot = RedHotTrippleSlotMachine.GetResourceById("Empty");

        if (_clampedDown)
        {
            if (slots[1].type == RedHotTrippleSlotType.Empty)
            {
                slots[0].GetRandom(true);
                slots[4].GetRandom(true);
            }
            else
            {
                slots[0].SetType(extraSlot.Value);
                slots[4].SetType(extraSlot.Value);
            }
        }
        else
        {
            if (slots[2].type == RedHotTrippleSlotType.Empty)
            {
                slots[1].GetRandom(true);
                slots[5].GetRandom(true);
            }
            else
            {
                slots[1].SetType(extraSlot.Value);
                slots[5].SetType(extraSlot.Value);
            }
        }

        StopAllCoroutines();
    }

    public RedHotTrippleSlotType GetSlotType(int index)
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

        if (_spinSettings.endSpin == RedHotTrippleSpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        if (_spinSettings.spinDirection == RedHotTrippleSpinDirection.Random)
        {
            _spinDirection = Random.Range(0, 2) == 0 ? RedHotTrippleSpinDirection.Downwards : RedHotTrippleSpinDirection.Upwards;
        }
        else
        {
            _spinDirection = _spinSettings.spinDirection;
        }

        _acceleration = acceleration <= 0 ? RedHotTrippleGameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? RedHotTrippleGameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
        _yOffset = 0f;
        _increaseSpeed = true;
        _inClamp = false;
        _inSpin = true;
        _forceStop = false;
    }

    public void Stop()
    {
        allowSymbolChanges = false;
        _inClamp = true;
        _inSpin = false;

        var xPos = _rectTransform.anchoredPosition.x;
        var topPos = Vector2.zero;
        var bottomPos = Vector2.zero;

        if (_spinDirection == RedHotTrippleSpinDirection.Downwards)
        {
            topPos = new Vector2(xPos, _spinSettings.middleBoundary);
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
        else
        {
            topPos = new Vector2(xPos, _spinSettings.topBoundary);
            bottomPos = new Vector2(xPos, _spinSettings.middleBoundary);

            var disTop = Vector3.Distance(_rectTransform.anchoredPosition, topPos);
            var disBot = Vector3.Distance(_rectTransform.anchoredPosition, bottomPos);

            if (Vector3.Distance(_rectTransform.anchoredPosition, topPos) <
                Vector3.Distance(_rectTransform.anchoredPosition, bottomPos))
            {
                //clamp to top
                _targetPos = topPos;
                _clampedDown = true;
            }
            else
            {
                //clamp to bottom
                _targetPos = bottomPos;
                _clampedDown = false;
            }
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
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _spinSettings.middleBoundary);

            //shift types
            if (_spinDirection == RedHotTrippleSpinDirection.Downwards)
            {
                for (var i = slots.Count - 1; i > 0; i--)
                {
                    var res = slots[i - 1].currentResource;
                    slots[i].SetType(res);
                }

                //slots[0].SetType(WheelOfFortuneSlotMachine.GetResourceById("Empty").Value);
            }
            else
            {
                for (var i = 0; i < slots.Count - 1; i++)
                {
                    var res = slots[i + 1].currentResource;
                    slots[i].SetType(res);
                }

                slots[slots.Count - 1].SetType(RedHotTrippleSlotMachine.GetResourceById("Empty").Value);
            }
        }
    }

    private RedHotTrippleSlotResource GetNextGeneratedSymbol()
    {
        if (_nextIsRealSymbol)
        {
            _nextIsRealSymbol = false;
            var real = RedHotTrippleSlotMachine.CachedRealSymbols;
            return real[Random.Range(0, real.Count)];
        }
        else
        {
            _nextIsRealSymbol = true;
            return RedHotTrippleSlotMachine.CachedEmptySymbol.Value;
        }
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

        if (_spinDirection == RedHotTrippleSpinDirection.Downwards)
        {
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
                    //ApplyReelOffset();
                    //generate new
                    slots[0].SetType(GetNextGeneratedSymbol());
                }
            }
        }
        else
        {
            _rectTransform.Translate(Vector3.up * (_currentSpeed * Time.deltaTime));

            var currentPos = _rectTransform.anchoredPosition;
            if (currentPos.y >= _spinSettings.middleBoundary)
            {
                //reset pos
                _yOffset = _spinSettings.middleBoundary - currentPos.y;

                _rectTransform.anchoredPosition = new Vector2(currentPos.x, _spinSettings.bottomBoundary + _yOffset);

                if (_inSpin && allowSymbolChanges && finalResultSymbols == null)
                {
                    //shift types
                    for (var i = 0; i < slots.Count - 1; i++)
                    {
                        var res = slots[i + 1].currentResource;
                        slots[i].SetType(res);
                    }
                    //ApplyReelOffset();

                    //generate new
                    slots[slots.Count - 1].SetType(GetNextGeneratedSymbol());
                }
            }
        }
    }
    #endregion
}
