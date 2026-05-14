using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer)), RequireComponent(typeof(Collider2D))]
public class WorldSpaceUIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private SpriteRenderer spriteRenderer;

    public Sprite sprite;
    public Sprite pressedSprite;
    public Sprite disabledSprite;

    public Color normalColor = Color.white;
    public Color disabledColor = new Color(128, 128, 128, 255);

    public bool interactable = true;

    public UnityEvent onClick;

    private bool isPressed = false;

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && sprite == null)
            sprite = spriteRenderer.sprite;
    }

    private void Awake()
    {
        Initialize();
    }
    private void Initialize()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && sprite == null)
            sprite = spriteRenderer.sprite;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        ButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ButtonUp();
    }

    void ButtonDown()
    {
        if (!interactable) return;

        isPressed = true;
        if (pressedSprite != null) spriteRenderer.sprite = pressedSprite;

        onClick.Invoke();
    }

    void ButtonUp()
    {
        if (!isPressed) return;

        SetInteractable(interactable);
    }

    public void SetInteractable(bool state)
    {
        Initialize();
        interactable = state;
        if (state)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.color = normalColor;
        }
        else
        {
            if (disabledSprite != null)
            {
                spriteRenderer.sprite = disabledSprite;
            }
            else
            {
                if (sprite != null) spriteRenderer.sprite = sprite;
                spriteRenderer.color = disabledColor;
            }
        }
    }

    public void SetActive(bool state)
    {
        gameObject.SetActive(state);
    }
}
