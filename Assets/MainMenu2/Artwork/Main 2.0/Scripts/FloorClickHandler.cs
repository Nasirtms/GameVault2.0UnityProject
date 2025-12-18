using UnityEngine;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(Collider))]
public class FloorClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
{
    public enum InputMode
    {
        ClickAndDrag,   // allows both click and drag
        DragOnly        // ignores click, only allows drag
    }

    [Header("References")]
    [SerializeField] private Player player;

    [Header("Settings")]
    [SerializeField] private float clickMoveDuration = 0.5f;
    [SerializeField] public float dragSensitivity = 0.03f;
    [SerializeField] public float dragMoveDuration = 0.1f;
    [SerializeField] private float dragThreshold = 10f; // pixels to detect drag vs click
    [SerializeField] private InputMode inputMode = InputMode.ClickAndDrag;

    public bool isDragging;
    private Vector2 pointerDownPos;
    public Vector2 lastDragPos;


    public void OnPointerDown(PointerEventData eventData)
    {
        //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(eventData.pointerId)) return;

        if (player.switchingEnvironment) return;

        pointerDownPos = eventData.position;
        lastDragPos = eventData.position;
        isDragging = false;
        MainMenuManager.instance.isDragging = false;
        MainMenuManager.instance.draggingwhileMovingWithButtons = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //if (player.CurrentState == Player.playerState.Walking) return;
        if (player.switchingEnvironment) return;
        isDragging = false;
        MainMenuManager.instance.isDragging = false;
        MainMenuManager.instance.draggingwhileMovingWithButtons = false;
        //player.SetIsDragging(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        //if (player.CurrentState == Player.playerState.Walking) return;
        if (inputMode == InputMode.DragOnly || inputMode == InputMode.ClickAndDrag)
        {
            Vector2 delta = eventData.position - lastDragPos;

            // mark as dragging once moved past threshold
            if (!isDragging && (eventData.position - pointerDownPos).sqrMagnitude > dragThreshold * dragThreshold)
            {
                isDragging = true;
                MainMenuManager.instance.isDragging = true;
                MainMenuManager.instance.draggingwhileMovingWithButtons = player.movingByButtons;
            }

            lastDragPos = eventData.position;

            if (!isDragging) return;

            player.movingByButtons = false;
            player.StopMove();
            if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
                player.clickTargetIndicator.SetParent(player.transform);

            player.clickTargetIndicator.position = new Vector3(player.transform.position.x, (player.transform.position.y + player.ClickIndicatorOffsetY), player.transform.position.z);

            float moveAmount = -delta.x * dragSensitivity;


            if (Mathf.Abs(moveAmount) > 0.001f)
            {
                if (player.Axis == Player.MoveAxis.Z)
                    player.MoveBy(moveAmount, dragMoveDuration,true);
                else
                    player.MoveBy(moveAmount, dragMoveDuration,true);

                player.SetIsDragging(true);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        //if (player.CurrentState == Player.playerState.Walking) return;
        if (inputMode == InputMode.ClickAndDrag)
        {
            if (!isDragging)
            {
                if (player.movingByButtons)
                {
                    MainMenuManager.instance.tapMoveWhileMovingWithButtons = true;
                }
                player.movingByButtons = false;
                

                Ray ray = Camera.main.ScreenPointToRay(eventData.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 target = hit.point;
                    target.y = player.transform.position.y;
                    player.MoveToClick(target - new Vector3(0, 1.43f, 0), clickMoveDuration);
                    
                }
            }
            else
            {
                if (!MainMenuManager.instance.draggingwhileMovingWithButtons)
                {
                    //player.PlayerWalkComplete(false);
                }
            }
        }

        isDragging = false;
        MainMenuManager.instance.isDragging = false;
        MainMenuManager.instance.draggingwhileMovingWithButtons = false;
    }
}
