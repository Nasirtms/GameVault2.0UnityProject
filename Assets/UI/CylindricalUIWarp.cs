using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

//[ExecuteAlways]
public class CylindricalUIWarpSwipe : MonoBehaviour
{
    [Header("Cylinder Settings")]
    public float radius = 500f;
    public float angleRange = 60f;
    public float dragSensitivity = 0.2f;
    public float verticalSpacing = 150f;
    public int rowCount = 2;
    public int visibleColumnCount = 5;

    public float snapSpeedMin;
    public float snapSpeedMax;
    public float minAngle;

    //[Header("Camera")]
    //public GameObject targetObject;

    [Header("Behavior")]
    public bool invertDragDirection = false;

    private RectTransform rectTransform;
    private float centerAngle = 0f;
    private Vector2 lastDragPos;
    [SerializeField] private bool isDragging;
    public bool IsDragging => isDragging;
    //[SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private bool potentialClick = false;
    [SerializeField] private float dragThreshold = 10f;

    private int activeTouchId = -1;

    public static bool isDragable = true;
    public static CylindricalUIWarpSwipe Instance { get; private set; }

    //private void Awake()
    //{
    //    Instance = this;
    //}

    //void Start()
    //{
    //    rectTransform = GetComponent<RectTransform>();
    //    //raycaster = transform.root.GetComponent<GraphicRaycaster>();
    //}

    //void Update()
    //{
    //    HandleInput();
    //}
    //private void LateUpdate()
    //{
    //    if (rectTransform == null || targetObject == null) return;

    //    int childCount = rectTransform.childCount;
    //    int totalColumnCount = Mathf.CeilToInt(childCount / (float)rowCount);
    //    float autoSpacing = angleRange / (visibleColumnCount - 1);
    //    float totalAngle = (totalColumnCount - 5f) * autoSpacing;


    //    float maxAngle = totalAngle;

    //    // Allow elastic swipe beyond bounds
    //    centerAngle = Mathf.Clamp(centerAngle, minAngle - autoSpacing * 2, maxAngle + autoSpacing * 2);
    //    float startAngle = -centerAngle; // <-- flip orientation to start at left

    //    for (int i = 0; i < childCount; i++)
    //    {
    //        RectTransform child = rectTransform.GetChild(i) as RectTransform;
    //        int col = i / rowCount;
    //        int row = i % rowCount;

    //        float angleDeg = startAngle + col * autoSpacing;
    //        float angleRad = angleDeg * Mathf.Deg2Rad;

    //        float x = Mathf.Sin(angleRad) * radius;
    //        float z = -Mathf.Cos(angleRad) * radius + radius;
    //        float y = (rowCount == 1) ? 0f : (row - 0.5f) * verticalSpacing;

    //        child.localPosition = new Vector3(x, y, z);

    //        // Face the camera
    //        Vector3 worldPos = child.position;
    //        Vector3 camPos = targetObject.transform.position;
    //        Vector3 lookDir = new Vector3(camPos.x, worldPos.y, camPos.z) - worldPos;
    //        if (lookDir != Vector3.zero)
    //            child.rotation = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0f, 180f, 0f);

    //    }

    //    /* // Elastic snap-back
    //     if (!isDragging)
    //     {
    //         if (centerAngle < minAngle)
    //             centerAngle = Mathf.Lerp(centerAngle, minAngle, min);
    //         else if (centerAngle > maxAngle)
    //             centerAngle = Mathf.Lerp(centerAngle, maxAngle, max);
    //     }*/

    //    if (!isDragging)
    //    {
    //        if (centerAngle < minAngle)
    //            centerAngle = Mathf.Lerp(centerAngle, minAngle, snapSpeedMin * Time.deltaTime);
    //        else if (centerAngle > maxAngle)
    //            centerAngle = Mathf.Lerp(centerAngle, maxAngle, snapSpeedMax * Time.deltaTime);
    //    }
    //}

    //void HandleInput()
    //{
    //    if (!isDragable) return;

    //    // If a second finger comes down while we're already tracking one,
    //    // just bail out completely until fingers clear.
    //    if (Input.touchCount > 1 && activeTouchId != -1)
    //    {
    //        // cancel current drag
    //        isDragging = false;
    //        potentialClick = false;

    //        return;
    //    }

    //    float direction = invertDragDirection ? 1f : -1f;

    //    // --- Touch input with single-finger tracking ---
    //    if (Input.touchCount > 0)
    //    {
    //        // Find the touch we should care about
    //        foreach (var touch in Input.touches)
    //        {
    //            // if no active touch yet, grab the first one that began
    //            if (touch.phase == TouchPhase.Began && activeTouchId == -1)
    //            {
    //                activeTouchId = touch.fingerId;
    //                lastDragPos = touch.position;
    //                isDragging = false;
    //                potentialClick = true;

    //                if (raycaster) raycaster.ignoreReversedGraphics = false;
    //            }

    //            // ignore any touches that aren't our active one
    //            if (touch.fingerId != activeTouchId)
    //                continue;

    //            // moving
    //            if (touch.phase == TouchPhase.Moved)
    //            {
    //                if (potentialClick && Vector2.Distance(touch.position, lastDragPos) > dragThreshold)
    //                {
    //                    isDragging = true;
    //                    potentialClick = false;

    //                    if (raycaster) raycaster.ignoreReversedGraphics = true;
    //                }

    //                if (isDragging)
    //                {
    //                    float delta = touch.position.x - lastDragPos.x;
    //                    centerAngle += delta * dragSensitivity * direction;
    //                    lastDragPos = touch.position;
    //                }
    //            }
    //            // ended or cancelled—clear our active touch
    //            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
    //            {
    //                if (touch.fingerId == activeTouchId)
    //                {
    //                    isDragging = false;
    //                    potentialClick = false;
    //                    activeTouchId = -1;

    //                    if (raycaster) raycaster.ignoreReversedGraphics = false;
    //                }
    //            }
    //        }
    //    }

    //    // --- Mouse input stays the same, if you still want desktop testing ---
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        lastDragPos = Input.mousePosition;
    //        isDragging = false;
    //        potentialClick = true;

    //        if (raycaster) raycaster.ignoreReversedGraphics = false;
    //    }
    //    else if (Input.GetMouseButton(0))
    //    {
    //        if (potentialClick && Vector2.Distance((Vector2)Input.mousePosition, lastDragPos) > dragThreshold)
    //        {
    //            isDragging = true;
    //            potentialClick = false;

    //            if (raycaster) raycaster.ignoreReversedGraphics = true;
    //        }

    //        if (isDragging)
    //        {
    //            float delta = Input.mousePosition.x - lastDragPos.x;
    //            centerAngle += delta * dragSensitivity * direction;
    //            lastDragPos = Input.mousePosition;
    //        }
    //    }
    //    else if (Input.GetMouseButtonUp(0))
    //    {
    //        isDragging = false;
    //        potentialClick = false;

    //        if (raycaster) raycaster.ignoreReversedGraphics = false;
    //    }
    //}
}