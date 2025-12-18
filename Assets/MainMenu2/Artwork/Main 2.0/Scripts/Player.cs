using DG.Tweening;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    public enum MoveAxis { X, Z }

    public enum playerState { Idle, Walking }

    [Header("Movement")]
    [SerializeField] private MoveAxis axis = MoveAxis.Z;
    [SerializeField] public float clampMin = -10f;
    [SerializeField] public float clampMax = 10f;
    [SerializeField] private float defaultDuration = 0.35f;
    [SerializeField] private Ease ease = Ease.Linear;
    public Transform moveTarget;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] public Transform clickTargetIndicator; // circle visual
    [SerializeField] private float clickIndicatorTweenTime = 0.2f;
    [SerializeField] private SpriteRenderer playerEyes;
    [SerializeField] private SpriteRenderer playerEyeBalls;
    [SerializeField] private SpriteRenderer playerEyesClose;
    [SerializeField] private SpriteRenderer playerMouth;
    [SerializeField] public float ClickIndicatorOffsetY = -5.22f;

    public bool movingByButtons = false;
    public bool switchingEnvironment = false;

    public event Action<Vector3, float> MoveScheduled;
    public Vector3 LastPlannedTarget { get; private set; }
    public playerState CurrentState { get; private set; } = playerState.Idle;

    private Tween _activeTween;
    public event Action MoveStarted;
    public event Action MoveCompleted;

    bool isWalking = false;
    bool reachedTarget = false;

    public MoveAxis Axis => axis;

    private void Awake()
    {
        if (animator != null)
        {
            animator.SetBool("idle", true);
            animator.SetBool("walk", false);
        }
        moveTarget.position = transform.position;
    }
    private void Start()
    {
        MoveTo(0, 1.5f);
    }

    private void Update()
    {
        if (CurrentState == playerState.Walking)
        {
            if (Mathf.Abs(transform.position.x - moveTarget.position.x) < 0.01f)
            {
                reachedTarget = true;
                PlayerWalkComplete();
                MainMenuManager.instance.tapMoveWhileMovingWithButtons = false;
            }
            else
            {
                reachedTarget = false;
            }
        }
    }

    public void CancelActiveMove()
    {
        if (_activeTween != null && _activeTween.IsActive()) _activeTween.Kill();
        _activeTween = null;
    }

    public void MoveBy(float delta, float duration = -1f, bool isDragging = false)
    {
        Vector3 t;
        t = transform.position;
        Debug.Log("MOOOVVEE: MainMenuManager.instance.isDragging: " + MainMenuManager.instance.isDragging);
        if (MainMenuManager.instance.isDragging)
        {
            t.x = moveTarget.position.x;
        }
        Debug.Log("MOOOVVEE: t.x: " + t.x);
        float d = duration > 0f ? duration : defaultDuration;

        CancelActiveMove();

        Vector3 target;
        if (axis == MoveAxis.Z)
        {
            float targetZ = Mathf.Clamp(t.z + delta, clampMin, clampMax);
            target = new Vector3(t.x, t.y, targetZ);
        }
        else
        {
            float targetX = Mathf.Clamp(t.x + delta, clampMin, clampMax);
            target = new Vector3(targetX, t.y, t.z);
            Debug.Log("MOOOVVEE: MoveBy: " + targetX);
        }

        moveTarget.position = target;

        UpdateFlip(target - transform.position);
        RunTween(target, d, isDragging);
    }

    public void MoveTo(float coord, float duration = -1f, bool isDragging=false)
    {
        var t = transform.position;
        Debug.Log("Current Position : " + t);
        float d = duration > 0f ? duration : defaultDuration;

        CancelActiveMove();

        Vector3 target;
        if (axis == MoveAxis.Z)
        {
            float targetZ = Mathf.Clamp(coord, clampMin, clampMax);
            target = new Vector3(t.x, t.y, targetZ);
        }
        else
        {
            float targetX = Mathf.Clamp(coord, clampMin, clampMax);
            target = new Vector3(targetX, t.y, t.z);
            Debug.Log("Target : " + target);
        }

        moveTarget.position = target;

        UpdateFlip(target - transform.position);
        RunTween(target, d , isDragging);
    }

    public void StopMove()
    {
        if (_activeTween != null && _activeTween.IsActive()) _activeTween.Kill();
        _activeTween = null;
        if (animator != null)
        {
            SetIsDragging(false);
        }
        CurrentState = playerState.Idle;
        MoveCompleted?.Invoke();
    }

    private void RunTween(Vector3 target, float duration,bool isDragging=false)
    {
        CurrentState = playerState.Walking;
        LastPlannedTarget = target;
        if (movingByButtons)
        {
            MoveScheduled?.Invoke(target, (float)(duration * 0.4));
        }
        else
        {
            MoveScheduled?.Invoke(target, duration);
        }

        MoveStarted?.Invoke();

        // start walking animation
        if (animator != null)
        {
            if (!isDragging)
            {
                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
            }
        }

        _activeTween = transform.DOMove(target, 7)
            .SetEase(ease)
            .SetSpeedBased(true)
            .OnComplete(() =>
            {
                if (!MainMenuManager.instance.isDragging)
                {
                    //PlayerWalkComplete(isDragging);
                    //if (animator != null)
                    //{
                    //    if (!isDragging)
                    //    {
                    //        animator.SetBool("walk", false);
                    //        animator.SetBool("idle", true);
                    //    }

                    //}

                    //movingByButtons = false;
                    //MoveCompleted?.Invoke();
                    //CurrentState = playerState.Idle;
                }
            });
    }

    public void PlayerWalkComplete(bool isDragging = false)
    {
        if (animator != null)
        {
            //if (!isDragging)
            //{
                SetIsDragging(false);
                //animator.SetBool("walk", false);
                //animator.SetBool("idle", true);
            //}

        }
        Debug.Log("PlayerWalkComplete");
        movingByButtons = false;
        MoveCompleted?.Invoke();
        CurrentState = playerState.Idle;
    }
    public void SetIsDragging(bool isDrag) {
        animator.SetBool("isDragging", isDrag);
        animator.SetBool("walk", false);
        animator.SetBool("idle", !isDrag);
    }

    private void UpdateFlip(Vector3 moveDelta)
    {
        if (spriteRenderer == null) return;

        float dir = axis == MoveAxis.X ? moveDelta.x : moveDelta.z;

        if (Mathf.Abs(dir) < 0.001f) return;

        spriteRenderer.flipX = dir < 0f;
    }

    /// <summary>
    /// Called externally by FloorClickHandler when user clicks on floor.
    /// </summary>
    public void MoveToClick(Vector3 worldTarget, float duration)
    {
        // Move indicator immediately (in world space)
        if (clickTargetIndicator != null && worldTarget.x <= clampMax && worldTarget.x >= clampMin)
        {

            if (clickTargetIndicator != null && clickTargetIndicator.parent == transform)
                clickTargetIndicator.SetParent(null);

            clickTargetIndicator
                .DOMove(worldTarget, clickIndicatorTweenTime)
                .SetEase(Ease.OutQuad);


            // Move player normally
            if (axis == MoveAxis.Z)
                MoveTo(worldTarget.z, duration);
            else
                MoveTo(worldTarget.x, duration);
        }
    }

    public void FadePlayer(float alpha , float duration)
    {
        spriteRenderer.DOFade(alpha, duration);
        playerEyeBalls.DOFade(alpha, duration);
        playerEyes.DOFade(alpha, duration);
        playerEyesClose.DOFade(alpha, duration);
        playerMouth.DOFade(alpha, duration);
    }

    public void FlipPlayer(bool flip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
    }
}
