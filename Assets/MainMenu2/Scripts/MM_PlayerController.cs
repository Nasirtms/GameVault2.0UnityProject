using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MainMenu
{
    public class MM_PlayerController : MonoBehaviour
    {
        public List<GameObject> playerAvatarsObjects;
        public Sprite[] playerProfilePictures;
        public int currentPlayerIndex;

        public Animator animator;
        public List<SpriteRenderer> avatarSpriteRenderers = new List<SpriteRenderer>();

        public Transform followTarget;
        public float moveSpeed = 5;
        public Vector2 moveSpeedMinMax = new Vector2(4, 100);
        public bool isWalking = false;
        //public Vector3 offset = new Vector3(0f, 0f, 0f);

        public bool followActive = true;
        bool movedIntoBoundsOnce = false;
        bool reachedTarget = false;

        Vector3 targetEulerAngles;
        bool isGoingIn;

        private void Awake()
        {
            SetCurrentPlayer(currentPlayerIndex);
        }


        private void ActiveAvatar()
        {
            avatarSpriteRenderers.Clear();
            avatarSpriteRenderers = playerAvatarsObjects[currentPlayerIndex].GetComponentsInChildren<SpriteRenderer>(false).ToList();
        }

        private void Start()
        {
            LoadAllAvatarUnderPlayer();
            ActiveAvatar();
            targetEulerAngles = transform.localEulerAngles;
        }


        void LoadAllAvatarUnderPlayer()
        {
            if (UserManager.Instance != null)
            {
                currentPlayerIndex = UserManager.Instance.avatarIndex;
            }
            // Load avatars under player
            playerAvatarsObjects.Clear();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.name.Equals("AvatarCamera"))
                {
                    playerAvatarsObjects.Add(child.gameObject);
                }
            }
        }
        public void Initialize(int playerAvatarIndex = -1)
        {
            if (UserManager.Instance != null)
            {
                currentPlayerIndex = UserManager.Instance.avatarIndex;
            }
            transform.localScale = Vector3.one;
            SetAvatarFade(1f, 0.01f);
            //SetWalkAnimationState(false);
        }

        public void SetCurrentPlayer(int index)
        {
            for (int i = 0; i < playerAvatarsObjects.Count; i++)
            {
                playerAvatarsObjects[i].SetActive(false);
            }
            avatarSpriteRenderers.Clear();

            currentPlayerIndex = index;

            playerAvatarsObjects[currentPlayerIndex].SetActive(true);
            animator = playerAvatarsObjects[currentPlayerIndex].GetComponent<Animator>();
            avatarSpriteRenderers = playerAvatarsObjects[currentPlayerIndex].GetComponentsInChildren<SpriteRenderer>(false).ToList();

            SetAvatarFade(1f, 0.01f);
            SetWalkAnimationState(isWalking);
        }

        private void Update()
        {
            if (followActive)
            {
                if (followTarget == null)
                    return;
                
                if (Mathf.Abs(transform.position.x - followTarget.position.x) > 0f)
                    reachedTarget = false;
                else
                    reachedTarget = true;

                if (!reachedTarget)
                {
                    StartWalking();
                    SetAvatarDirection();
                }
                else
                {
                    MainMenuManager.instance.movingWithButtons = false;

                    if (!MainMenuManager.instance.isDragging)
                    {
                        StopWalking();
                    }
                }

                if (movedIntoBoundsOnce)
                    transform.position = new Vector3(Mathf.Clamp(Mathf.MoveTowards(transform.position.x, followTarget.position.x, Time.deltaTime * moveSpeed), MainMenuManager.instance.currentEnvironment.environmentBounds.x, MainMenuManager.instance.currentEnvironment.environmentBounds.y), transform.position.y, transform.position.z);
                else
                    transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, followTarget.position.x, Time.deltaTime * moveSpeed), transform.position.y, transform.position.z);
            }
        }

        void SetAvatarDirection()
        {
            if (followTarget == null) return;

            SetAvatarDirection(followTarget.position);
        }

        void SetAvatarDirection(Vector3 targetPosition)
        {
            targetEulerAngles.y = targetPosition.x >= transform.position.x ? 0 : -180;
            transform.localEulerAngles = targetEulerAngles;
        }

        public void SetAvatarDirection(bool right)
        {
            targetEulerAngles.y = right ? 0 : -180;
            transform.localEulerAngles = targetEulerAngles;
        }

        void StartWalking()
        {
            if (!isWalking)
            {
                isWalking = true;
                MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
                SetWalkAnimationState(true);
            }
        }

        void StopWalking()
        {
            if (isWalking)
            {
                isWalking = false;
                SetWalkAnimationState(false);

                MainMenuManager.instance.mainCamera.followTarget = transform;
                //MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.y;
            }
        }

        void SetWalkAnimationState(bool state)
        {
            animator.SetBool("idle", !state);
            animator.SetBool("walk", state);
        }

        public void SetAvatarFade(float value, float duration)
        {
            MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
            foreach (SpriteRenderer sr in avatarSpriteRenderers)
            {
                sr.DOKill();
                sr.DOFade(value, duration);
            }
        }

        public void GoToTarget(Vector3 targetPosition)
        {
            isWalking = true;
            SetWalkAnimationState(true);
            SetAvatarDirection(targetPosition);
        }

        public void GoInside(float time)
        {

            SetAvatarFade(0, time);
            transform.DOScale(0.8f, time);
        }

        public void GoInsideMachine(float time)
        {
            SetAvatarFade(0, time);
            MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
            //transform.DOMoveY(transform.position.y + 0.5f, time).SetEase(Ease.Linear);
            transform.DOScale(0.8f, time).SetEase(Ease.Linear);
        }
    }
}