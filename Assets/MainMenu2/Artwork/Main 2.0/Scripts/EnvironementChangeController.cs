using DG.Tweening;
using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

[RequireComponent(typeof(Collider2D))]
public class EnvironementChangeController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
{
    [Header("Button Click Event")]
    public UnityEvent onClick;

    [Header("Drag Settings")]
    public float dragThreshold = 10f;
    [SerializeField] private float dragMoveDuration;
    [SerializeField] private float dragSensitivity;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Player player;
    [SerializeField] private GameObject currentEnv;
    [SerializeField] private GameObject newEnv;
    [SerializeField] private GameObject loadConvas;
    [SerializeField] private PlayerMovementControllor playerMovementControllor;
    [SerializeField] private GameObject EntryPoint;

    private bool pointerDown = false;
    private bool isDragging = false;
    private Vector2 pointerDownPos;
    private Vector2 lastDragPos;
    private float entrancePoint;

    private void Start()
    {
        Debug.Log("jhony test: " + EntryPoint.transform.position.x);
        entrancePoint = EntryPoint.transform.position.x;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        if (!pointerDown) return;

        pointerDown = false;

        if (!isDragging)
        {
            //if (player.movingByButtons)
            //{
            //    MainMenuManager.instance.tapMoveWhileMovingWithButtons = true;
            //}
            player.movingByButtons = false;
            onClick?.Invoke();
        }
        else
        {
            //player.PlayerWalkComplete(isDragging);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        pointerDown = true;
        isDragging = false;
        MainMenuManager.instance.isDragging = false;
        MainMenuManager.instance.draggingwhileMovingWithButtons = false;
        pointerDownPos = eventData.position;
        lastDragPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        //if (player.CurrentState == Player.playerState.Walking) return;

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

        float moveAmount = -delta.x * dragSensitivity;

        if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
            player.clickTargetIndicator.SetParent(player.transform);

        player.clickTargetIndicator.position = new Vector3(player.transform.position.x, (player.transform.position.y + player.ClickIndicatorOffsetY), player.transform.position.z);

        if (Mathf.Abs(moveAmount) > 0.001f)
        {
            if (player.Axis == Player.MoveAxis.Z)
                player.MoveBy(moveAmount, dragMoveDuration, true);
            else
                player.MoveBy(moveAmount, dragMoveDuration, true);

            player.SetIsDragging(true);
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (player.switchingEnvironment) return;

        isDragging = false;
        MainMenuManager.instance.isDragging = false;
        MainMenuManager.instance.draggingwhileMovingWithButtons = false;
        pointerDown = false;
        //player.SetIsDragging(false);
    }



    public void RemovePlayerPositionIndicatorFromPlayer()
    {
        if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == player.transform )
        {
            player.clickTargetIndicator.SetParent(null);
        }
    }

    public void AddPlayerPositionIndicatorToPlayer()
    {
        player.clickTargetIndicator.transform.position = new Vector3(0, -4.31f, 0f);
    }

    public void SwitchEnvironment()
    {
        if(player.CurrentState == Player.playerState.Walking) return;

        StartCoroutine(SwitchEnvironmentCoroutine());
    }

    private IEnumerator SwitchEnvironmentCoroutine()
    {
        player.switchingEnvironment = true;
        GotoEnteryPointOfCatagory();

        yield return new WaitForSeconds(1.5f);

        RemovePlayerPositionIndicatorFromPlayer();
        mainCamera.DOOrthoSize(3.5f, 1.6f);
        player.FadePlayer(0f, 1.6f);
        player.transform.DOScale(0.1f, 1.6f).SetEase(Ease.Linear);

        player.GetComponent<Animator>().SetBool("idle", false);
        player.GetComponent<Animator>().SetBool("walk", true);

        yield return new WaitForSeconds(2.3f);

        loadConvas.gameObject.SetActive(true);
        currentEnv.SetActive(false);
        mainCamera.orthographicSize = 4f;
        newEnv.SetActive(true);

        mainCamera.transform.position = new Vector3(0f, mainCamera.transform.position.y, mainCamera.transform.position.z);
        player.transform.position = new Vector3(-11f, player.transform.position.y, player.transform.position.z);
        player.transform.localScale = new Vector3(0.27f, 0.27f, 0.27f);
        player.FadePlayer(1f, 0f);

        loadConvas.gameObject.SetActive(false);
        AddPlayerPositionIndicatorToPlayer();

        player.MoveTo(0f, playerMovementControllor.stepDuration);
        EnvironmentExitController.Instance.ExitToCantagoryEnvironment.gameObject.SetActive(true);
        player.switchingEnvironment = false;
    }


    private void GotoEnteryPointOfCatagory()
    {
        if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
        {
            player.clickTargetIndicator.SetParent(player.transform);
        }

        player.GetComponent<Animator>().SetBool("idle", false);
        player.GetComponent<Animator>().SetBool("walk", true);
        Debug.Log("entery point x: " + (EntryPoint.transform.position.x));

        mainCamera.GetComponent<CameraFollowOnMove>().SetFollowActive(false);
        mainCamera.transform.DOMoveX(entrancePoint, 0.5f).SetEase(Ease.InOutSine);

        if (entrancePoint < player.transform.position.x)
        {
            player.FlipPlayer(true);
        }
        else
        {
            player.FlipPlayer(false);
        }

        player.transform.DOMoveX(EntryPoint.transform.position.x - player.transform.position.x, 1.5f).SetEase(Ease.InOutSine);
    }

    public void OnExit()
    {
        StartCoroutine(OnExitCatagory());
    }

    private IEnumerator OnExitCatagory()
    {
        loadConvas.gameObject.SetActive(true);
        currentEnv.SetActive(true);
        newEnv.SetActive(false);

        yield return new WaitForSeconds(0.7f);

        loadConvas.gameObject.SetActive(false);

        yield return null;
    }
}
