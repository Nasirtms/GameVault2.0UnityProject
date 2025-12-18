using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollowOnMove : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
    public Transform followTarget;
    public Vector3 followTargetPosition;
    public bool reachedTarget = false;

    [Header("Easing")]
    [SerializeField] private Ease ease = Ease.Linear;
    public FloorClickHandler floor;
    public FloorClickHandler Dragger;
    public Vector2 followDampingMinMax = new Vector2(3, 100);
    public float followDamping;

    Tween _camTween;
    //Tween _camTween_GoToPlayer;
    bool followActive = false;
    bool followPlayer = false;

    void OnEnable()
    {
        if (player != null) player.MoveScheduled += OnPlayerMoveScheduled;
    }
    void OnDisable()
    {
        if (player != null) player.MoveScheduled -= OnPlayerMoveScheduled;
    }

    public void SetFollowActive(bool state)
    {
        followActive = state;
    }

    private void LateUpdate()
    {
        if (followActive)
        {
            if (!MainMenuManager.instance.draggingwhileMovingWithButtons || followPlayer)
            {
                followTargetPosition = player.transform.position;
            }
            Vector3 camTarget = followTargetPosition + offset;
            transform.position = new Vector3(Mathf.Clamp(Mathf.Lerp(transform.position.x, camTarget.x, Time.deltaTime * followDamping), player.clampMin, player.clampMax), transform.position.y, transform.position.z);
            if (Mathf.Abs(transform.position.x - camTarget.x) < 0.01f)
            {
                reachedTarget = true;
                if (followPlayer)
                {
                    followDamping = followDampingMinMax.y;
                }
            }
            else
            {
                reachedTarget = false;
            }
        }
    }

    //public void GoToPlayer()
    //{
    //    _camTween = transform.DOMoveX(camTarget.x, duration).SetEase(ease);
    //}

    void OnPlayerMoveScheduled(Vector3 target, float followSpeed)
    {
        if (_camTween != null && _camTween.IsActive()) _camTween.Kill();
        Vector3 camTarget = target + offset;
        // Lock whichever axis you don’t want to change:
        // Example: follow only X or Z but keep current Y:
        camTarget.y = transform.position.y;
        Debug.Log("ct: "+ camTarget);
        //followingPlayer = true;
        //followTarget = target;
        //followDamping = followSpeed;

        if (!floor.isDragging && !Dragger.isDragging)
        {
            if (player.movingByButtons)
            {
                followActive = false;
                followPlayer = false;
                _camTween = transform.DOMoveX(camTarget.x, followSpeed).SetEase(ease);
            }
            else
            {
                followActive = true;
                followPlayer = true;
                if (MainMenuManager.instance.tapMoveWhileMovingWithButtons)
                {
                    followDamping = followDampingMinMax.x;
                }
                else
                {
                    followDamping = followDampingMinMax.y;
                }

                if (Mathf.Abs(transform.position.x - camTarget.x) < 0.01f)
                {
                    reachedTarget = true;
                }
                else
                {
                    reachedTarget = false;
                }

            }
        }
        else
        {
            //_camTween = transform.DOMoveX(camTarget.x, followSpeed).SetEase(ease);
            if (!MainMenuManager.instance.draggingwhileMovingWithButtons)
            {
                followTargetPosition = player.transform.position;
                followDamping = followDampingMinMax.y;
                Debug.Log("UPP");
            }
            else
            {
                followTargetPosition = target;
                followDamping = followDampingMinMax.x;
                Debug.Log("followDamping: " + followDamping);
            }
            followActive = true;
        }
    }
}
