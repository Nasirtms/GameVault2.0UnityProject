using TMPro;
using UnityEngine;

public class NotificationEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public void SetMessage(string msg)
    {
        //Debug.Log($"[ENTRY] SetMessage called: {msg}");
        messageText.text = msg;
    }
}
