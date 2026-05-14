using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class IrishPotLuckRulesPopupController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;

    [Header("Horizontal Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private float snapSpeed = 12f;
    [SerializeField] private float minDragDistance = 20f;

    [Header("Page Indicators")]
    [SerializeField] private Image[] pageDots;
    [SerializeField] private Sprite activeDotSprite;
    [SerializeField] private Sprite inactiveDotSprite;

    private float[] pagePositions;
    private int totalPages;
    private int currentPage = 0;
    private bool isDragging = false;
    private Vector2 dragStartPos;

    private void Start()
    {
        //rulesPopupPanel.SetActive(false);
        closeButton.onClick.AddListener(ClosePopup);

        if (scrollRect != null)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
        }

        SetupPages();
        GoToPage(0, true);
        UpdatePageIndicators();
    }

    private void Update()
    {
        if (!isDragging && scrollRect != null && pagePositions != null && pagePositions.Length > 0)
        {
            float target = pagePositions[currentPage];
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                target,
                Time.deltaTime * snapSpeed
            );
        }
    }

    private void SetupPages()
    {
        if (content == null)
            return;

        totalPages = content.childCount;

        if (totalPages <= 0)
            totalPages = 1;

        pagePositions = new float[totalPages];

        if (totalPages == 1)
        {
            pagePositions[0] = 0f;
            return;
        }

        for (int i = 0; i < totalPages; i++)
        {
            pagePositions[i] = (float)i / (totalPages - 1);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        SetupPages();
        currentPage = 0;
        GoToPage(currentPage, true);
        UpdatePageIndicators();
    }

    public void ClosePopup()
    {
        rulesPopupPanel.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPos = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        float dragDeltaX = eventData.position.x - dragStartPos.x;

        if (Mathf.Abs(dragDeltaX) >= minDragDistance)
        {
            if (dragDeltaX < 0f)
                currentPage = Mathf.Min(currentPage + 1, totalPages - 1);
            else
                currentPage = Mathf.Max(currentPage - 1, 0);
        }
        else
        {
            currentPage = GetClosestPage();
        }

        UpdatePageIndicators();
        GoToPage(currentPage, false);
    }

    private int GetClosestPage()
    {
        float currentPos = scrollRect.horizontalNormalizedPosition;
        int closestPage = 0;
        float closestDistance = Mathf.Abs(currentPos - pagePositions[0]);

        for (int i = 1; i < pagePositions.Length; i++)
        {
            float distance = Mathf.Abs(currentPos - pagePositions[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPage = i;
            }
        }

        return closestPage;
    }

    private void GoToPage(int pageIndex, bool instant = false)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        if (scrollRect == null || pagePositions == null || pagePositions.Length == 0)
            return;

        if (instant)
            scrollRect.horizontalNormalizedPosition = pagePositions[currentPage];

        UpdatePageIndicators();
    }

    private void UpdatePageIndicators()
    {
        if (pageDots == null || pageDots.Length == 0)
            return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] == null)
                continue;

            pageDots[i].sprite = (i == currentPage) ? activeDotSprite : inactiveDotSprite;
        }
    }
}