using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GoldenDragonUIButtonController : MonoBehaviour
{
    private Button button;

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