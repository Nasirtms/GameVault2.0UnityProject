using UnityEngine;

public class PopupSpawner : MonoBehaviour
{
    public GameObject popupPrefab; // Assign in Inspector or instantiate manually

    private void Start()
    {
        // Start invoking ShowPopup every 5 seconds, starting after 1 second
        InvokeRepeating(nameof(ShowPopup), 1f, 3f);
    }

    void ShowPopup()
    {
        // Instantiate a new popup, or enable it if you have a reusable one
        GameObject popup = Instantiate(popupPrefab, transform);
        popup.SetActive(true);

        // Optionally destroy it after a few seconds
        Destroy(popup, 1f);
    }
}
