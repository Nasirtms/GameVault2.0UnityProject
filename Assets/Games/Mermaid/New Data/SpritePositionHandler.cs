using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]  
public class SpritePositionHandler : MonoBehaviour
{
    public UnityEvent OnSpriteClick;
    public Vector3 fixedPosition = new Vector3(0, 0, 0); 
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetSpritePosition(fixedPosition);
    }
    void Update()
    {
        if (Application.isPlaying)
        {
            if (transform.position != fixedPosition)
            {
                SetSpritePosition(fixedPosition);
            }
        }
        else
        {
            SetSpritePosition(fixedPosition);
        }
    }
    public void SetSpritePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
    public void FixSpritePosition()
    {
        transform.position = fixedPosition;
    }
    private void OnMouseDown()
    {
        OnSpriteClick?.Invoke();
    }
    public void TriggerOnClickEvent()
    {
        OnSpriteClick?.Invoke();
    }
}
