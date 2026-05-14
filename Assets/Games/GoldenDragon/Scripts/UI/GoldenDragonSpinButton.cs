using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GoldenDragonSpinButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Time in seconds to trigger a long press.")]
    public float longPressThreshold = 0.5f;
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;
    private bool longPressTriggered = false;

    private Button btn;

    private void Start()
    {
        btn = GetComponent<Button>();
    }

    void Update()
    {
        if (isPointerDown)
        {
            pointerDownTimer += Time.deltaTime;
            if (!longPressTriggered && pointerDownTimer >= longPressThreshold)
            {
                longPressTriggered = true;
                isPointerDown = false;
                OnLongPress(); // :white_tick: Call long press
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (btn.interactable == false) return;

        isPointerDown = true;
        pointerDownTimer = 0f;
        longPressTriggered = false;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (btn.interactable == false) return;

        if (isPointerDown && !longPressTriggered)
        {
            OnPress(); // :white_tick: Call press
        }
        Reset();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        Reset();
    }
    private void Reset()
    {
        isPointerDown = false;
        pointerDownTimer = 0f;
        longPressTriggered = false;
    }
    private void OnPress()
    {
        GoldenDragonUIManager.Instance.OnClickSpin();
        //Debug.Log("Pressed");

    }
    private void OnLongPress()
    {
        GoldenDragonUIManager.Instance.OnHoldSpin();
        Debug.Log("Long Pressed");
    }
}

