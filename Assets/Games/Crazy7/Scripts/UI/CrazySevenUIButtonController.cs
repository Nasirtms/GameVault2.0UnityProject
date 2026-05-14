using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CrazySevenUIButtonController : MonoBehaviour
{
    public Button button;

    private void Awake()
    {
        TryGetButton();
    }

    private void TryGetButton()
    {
        if (button == null && this != null && gameObject != null)
        {
            button = GetComponent<Button>();
        }
    }

    public void ShowButton(bool show)
    {
        if (this == null || gameObject == null) return;

        TryGetButton();
        if (button != null)
        {
            button.gameObject.SetActive(show);
            button.interactable = show;
        }
    }

    public void SetButtonInteractable(bool interactable)
    {
        if (this == null || gameObject == null) return;

        TryGetButton();
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public Button GetButtonComponent()
    {
        TryGetButton();
        return button;
    }

    private void OnDestroy()
    {
        button = null;
    }
}