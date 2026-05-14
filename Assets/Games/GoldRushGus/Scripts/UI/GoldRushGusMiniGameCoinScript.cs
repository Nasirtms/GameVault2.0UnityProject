using UnityEngine;
using UnityEngine.UI;

public class GoldRushGusMiniGameCoinScript : MonoBehaviour
{
    #region Variables

    public Button button;
    public RectTransform frontImage;
    public RectTransform backImage;

    public RectTransform root;
    public RectTransform visual;
    private Image backGraphic;
    public bool revealed;
    #endregion

    #region Public References
    private void Awake()
    {
        button = GetComponent<Button>();
        root = GetComponent<RectTransform>();
        visual = transform.GetChild(0).GetComponent<RectTransform>();

        backImage = visual.GetChild(0).GetComponent<RectTransform>();
        frontImage = visual.GetChild(1).GetComponent<RectTransform>();

        backGraphic = backImage.GetComponent<Image>();
        DisableRaycasts(frontImage);
        DisableRaycasts(backImage);

        ResetView();
    }

    private void DisableRaycasts(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (img != null)
            img.raycastTarget = false;
    }

    public void ResetView()
    {
        revealed = false;

        frontImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);

        frontImage.localScale = Vector3.one;
        backImage.localScale = Vector3.one;

        frontImage.localEulerAngles = Vector3.zero;
        backImage.localEulerAngles = Vector3.zero;

        button.interactable = true;
    }
    public void SetBackSprite(Sprite sprite)
    {
        if (backGraphic != null)
            backGraphic.sprite = sprite;
    }
    public void Lock()
    {
        revealed = true;
        button.interactable = false;
    }
    #endregion
}