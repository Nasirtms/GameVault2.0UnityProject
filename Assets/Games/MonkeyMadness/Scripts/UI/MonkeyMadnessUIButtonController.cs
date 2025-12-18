using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MonkeyMadnessUIButtonController : MonoBehaviour
{
    private Button button;

    #region Unity Methods

    private void Awake()
    {
        TryGetButton();
    }
    
    private void OnDestroy()
    {
        button = null;
    }

    #endregion

    #region Geting Button

    private void TryGetButton()
    {
        if (button == null && this != null && gameObject != null)
        {
            button = GetComponent<Button>();
        }
    }
    
    public Button GetButtonComponent()
    {
        TryGetButton();
        return button;
    }

    #endregion

    #region Button State

    public void ShowButton(bool show)
    {
        if (this == null || gameObject == null) return;

        TryGetButton();
        if (button != null)
        {
            button.gameObject.SetActive(show);
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

    #endregion
}