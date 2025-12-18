using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;

public class FruitParadiseReelScript : MonoBehaviour
{
    public delegate void FruitParadiseReelEvents(int index);
    public static event FruitParadiseReelEvents OnSpinStart;
    public static event FruitParadiseReelEvents OnSpinComplete;

    [SerializeField] private VerticalLayoutGroup verticalLayout;
    [SerializeField] private List<FruitParadiseSlotType> slotsType = new List<FruitParadiseSlotType>();
    public List<FruitParadiseSlotScript> slots;

    //[SerializeField]
    private int _index;
    private bool _inSpin;
    private bool _inClamp;
    private float _yOffset;
    private RectTransform _rectTransform;
    private FruitParadiseSpinSettings _spinSettings;
    private Vector2 _targetPos;
    private float _currentSpeed;
    private bool _increaseSpeed;
    private float _delayAmount;
    private float _timeCounter;
    private bool _clampedDown;
    private float _acceleration;
    private bool _forceStop;
    private bool allowSymbolChanges = true;
    public bool canStopReel = false;
    private bool resultApplied;

    private List<SymbolData> finalResultSymbols;
    private void Start()
    {
        if (FruitParadiseSlotMachine.Instance != null)
            FruitParadiseSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (FruitParadiseSlotMachine.Instance != null)
            FruitParadiseSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    private void HandleStart()
    {
        //Debug.Log("Comming From Reel Script " + gameObject.name);
        StopAllCoroutines();
    }

    public void Initialize(int index)
    {
        FruitParadiseSlotMachine.Instance.StopReelProcess += HandleStart;
        _rectTransform = GetComponent<RectTransform>();
        this._forceStop = false;
        this._inSpin = false;
        this._index = index;

        if (slots == null || slots.Count != 4)
        {
            slots = new List<FruitParadiseSlotScript>(GetComponentsInChildren<FruitParadiseSlotScript>());
        }


        for (var i = 0; i < this.slots.Count; i++)
        {
            slots[i].Initialize(this, reelIndex, i); // ? this = parent reel, reelIndex = column, i = row
        }

        AssignUniqueRandomVisualsToReelSlots();

        _spinSettings = FruitParadiseSlotMachine.Instance.settings.spinSettings;
    }

    public int reelIndex;
    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= FruitParadiseSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }
        var symbols = FruitParadiseSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        resultApplied = true;
        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = FruitParadiseSlotMachine.GetResourceById(symbolData.id);
            int row = rowIndex;

            if (res.HasValue)
            {
                if (_clampedDown)
                {
                    row -= 1;
                }

                var slot = slots[row + 1]; 

                slot.SetType(res.Value);

                slot.ReelIndex = reelIndex;
                slot.RowIndex = rowIndex;

            }
            else
            {
                Debug.LogWarning($"?? No slot resource found for ID: {symbolData.id}");
            }
        }


        StopAllCoroutines();
    }

    public void ResetShape()
    {
        FruitParadiseSlotMachine.Instance.isResultReceived = false;
        foreach (var slot in slots)
        {
            slot.isResultSet = false;
            slot.ToggleSlotBorder(false);
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

    public FruitParadiseSlotType GetSlotType(int index)
    {
        return slots[index + 1].type;
    }

    public RectTransform GetSlotTransform(int index)
    {
        return slots[index + 1].gameObject.GetComponent<RectTransform>();
    }

    public void Spin(float delay, float acceleration, float speed)
    {
        canStopReel = false;
        resultApplied = false;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        if (_spinSettings.endSpin == FruitParadiseSpinType.All)
        {
            _delayAmount = 0f;
        }
        else
        {
            _delayAmount = _index * delay;
        }

        _acceleration = acceleration <= 0 ? FruitParadiseGameExtension.GetRandomValue(_spinSettings.acceleration) : acceleration;
        _timeCounter = 0f;
        _currentSpeed = speed <= 0f ? FruitParadiseGameExtension.GetRandomValue(_spinSettings.startSpeed) : speed;
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
        _targetPos = Vector3.Distance(_rectTransform.anchoredPosition, topPos) < Vector3.Distance(_rectTransform.anchoredPosition, bottomPos) ? topPos : bottomPos;
        _clampedDown = _targetPos == bottomPos;
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
        FruitParadiseUIManager.Instance.PlaySound("FruitParadise_ReelStop");
        OnSpinComplete?.Invoke(this._index);
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

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _acceleration * Time.deltaTime);
            float lerpFactor = Mathf.Clamp01(_currentSpeed * Time.deltaTime);
            _rectTransform.anchoredPosition = Vector3.LerpUnclamped(_rectTransform.anchoredPosition, _targetPos, lerpFactor);

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

        _rectTransform.Translate(Vector3.down * (_currentSpeed * Time.deltaTime));
        var currentPos = _rectTransform.anchoredPosition;
        if (currentPos.y <= _spinSettings.bottomBoundary)
        {
            _yOffset = _spinSettings.bottomBoundary - currentPos.y;
            _rectTransform.anchoredPosition = new Vector2(currentPos.x, _spinSettings.topBoundary + _yOffset);

            if (_inSpin && allowSymbolChanges && finalResultSymbols == null)
            {
                // slides up old values and injects new random one
                for (var i = slots.Count - 1; i > 0; i--)
                {
                    var res = slots[i - 1].currentResource;
                    if (!slots[i].isResultSet)
                    {
                        if (!FruitParadiseSlotMachine.Instance.isStopBtnPressed || !FruitParadiseSlotMachine.Instance.isResultReceived)
                        {
                            AssignUniqueRandomVisualsToReelSlots();
                        }
                    }
                }
            }
        }
    }

    private void AssignUniqueRandomVisualsToReelSlots()
    {
        Dictionary<string, HashSet<FruitParadiseSlotType>> reelSlotTypes = new();

        foreach (var slot in slots)
        {
            string reelName = slot.transform.parent.name;

            if (!reelSlotTypes.ContainsKey(reelName))
                reelSlotTypes[reelName] = new HashSet<FruitParadiseSlotType>();
            slot.ShowUniqueRandomVisual(reelSlotTypes[reelName]);
        }
    }
}
