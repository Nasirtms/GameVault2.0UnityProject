using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundHandler : MonoBehaviour
{
    public static BackgroundHandler Instance;
    public GameObject FirstBackground;
    public List<Sprite> bg_images = new List<Sprite>();

    [Header("Image Settings")]
    public RectTransform imageRect;  // Reference to the RectTransform of the image
    public float moveDuration = 2f;  // Duration for the movement
    public float moveDistance = 500f;

    [Header("Canvas Data")]
    public RectTransform canvasRect;
    private float canvasWidth;
    private float canvasHeight;

    Sprite currentImage;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }

        if (FirstBackground != null)
        {
            Sprite firstImage = FirstBackground.GetComponent<Image>().sprite;
            if (firstImage != null)
            {
                bg_images.Add(firstImage);
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if(canvasRect != null)
        {
            canvasWidth = canvasRect.rect.width;
            canvasHeight = canvasRect.rect.height;
        }

        if (FirstBackground != null)
        {
            currentImage = FirstBackground.GetComponent<Image>().sprite;
        }
    }

    [ContextMenu("HandleBackgroundChange")]
    public void HandleBackgroundChange()
    {
        int index = Random.Range(0, bg_images.Count);
        Sprite selectedImage = bg_images[index];

        switch (index)
        {
            case 0:
                MoveImageFromLeftToRight();
                break;
                case 1:
                    MoveImageFromRightToLeft();
                    break;
                case 2:
                    MoveImageFromTopToBottom();
                    break;
                case 4:
                    MoveImageFromBottomToTop();
                    break;
            default:
                break;
        }

        currentImage = selectedImage;
    }
    public void MoveImageFromLeftToRight()
    {
        // Start the image off-screen on the left side
        imageRect.anchoredPosition = new Vector2(-canvasWidth, imageRect.anchoredPosition.y);

        // Move it to the right corner of the canvas (canvasWidth)
        imageRect.DOAnchorPosX(canvasWidth, moveDuration).SetEase(Ease.Linear);
    }
    public void MoveImageFromRightToLeft()
    {
        // Start the image off-screen on the right side
        imageRect.anchoredPosition = new Vector2(canvasWidth, imageRect.anchoredPosition.y);

        // Move it to the left corner of the canvas (-canvasWidth)
        imageRect.DOAnchorPosX(-canvasWidth, moveDuration).SetEase(Ease.Linear);
    }
    public void MoveImageFromTopToBottom()
    {
        // Start the image off-screen at the top
        imageRect.anchoredPosition = new Vector2(imageRect.anchoredPosition.x, canvasHeight);

        // Move it to the bottom corner of the canvas (-canvasHeight)
        imageRect.DOAnchorPosY(-canvasHeight, moveDuration).SetEase(Ease.Linear);
    }
    public void MoveImageFromBottomToTop()
    {
        // Start the image off-screen at the bottom
        imageRect.anchoredPosition = new Vector2(imageRect.anchoredPosition.x, -canvasHeight);

        // Move it to the top corner of the canvas (canvasHeight)
        imageRect.DOAnchorPosY(canvasHeight, moveDuration).SetEase(Ease.Linear);
    }

}
