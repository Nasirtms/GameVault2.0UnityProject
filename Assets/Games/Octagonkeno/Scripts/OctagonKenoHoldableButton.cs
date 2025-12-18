using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class HoldableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Action onHoldComplete;
    private float holdTime;
    private Coroutine holdCoroutine;
    public bool IsHoldEnabled { get; set; } = true;

    public void Init(Action callback, float time)
    {
        onHoldComplete = callback;
        holdTime = time;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsHoldEnabled) return; // ✅ Ignore hold if disabled
        holdCoroutine = StartCoroutine(HoldTimer());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);
    }

    private IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(holdTime);
        onHoldComplete?.Invoke();
    }
}
