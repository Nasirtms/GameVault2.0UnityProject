using UnityEngine;
using TMPro;

public class BuildVersionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI versionText;

    void Start()
    {
        if (versionText != null)
        {
            // Example: "Version: 1.0.0 (Unity 2022.3.10f1)"
            versionText.text = $"Version: {Application.version}";
        }
        else
        {
            Debug.LogWarning("Version Text TMP is not assigned!");
        }
    }
}
