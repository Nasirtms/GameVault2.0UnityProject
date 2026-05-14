using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class MainMenuCamera : MonoBehaviour
    {
        public Camera camera;
        public Transform followTarget;
        public Vector3 offset = new Vector3(0f, 5f, -10f);
        public Vector2 followDampingMinMax = new Vector2(3, 100);
        public float followDamping;
        public Vector2 boundsMinMax;

        public bool followActive = true;

        bool reachedTarget = false;
        Vector3 startingPosition;
        float startingOrthographicSize;

        private void Awake()
        {
            camera = GetComponent<Camera>();
            startingPosition = transform.position;
            startingOrthographicSize = camera.orthographicSize;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            followActive = true;
            transform.position = new Vector3(startingPosition.x, transform.position.y, transform.position.z);
            camera.orthographicSize = startingOrthographicSize;
        }

        public void UpdateBounds(float min, float max)
        {
            boundsMinMax.x = min;
            boundsMinMax.y = max;
        }

        private void Update()
        {
            if (followActive)
            {
                if (followTarget == null)
                    return;

                Vector3 camTarget = followTarget.position + offset;
                transform.position = new Vector3(Mathf.Clamp(Mathf.Lerp(transform.position.x, camTarget.x, Time.deltaTime * followDamping), boundsMinMax.x, boundsMinMax.y), transform.position.y, transform.position.z);

                //if (Mathf.Abs(transform.position.x - camTarget.x) < 0.01f)
                if (MainMenuManager.instance.movingWithButtons)
                {
                    if (Mathf.Abs(transform.position.x - camTarget.x) > 0f)
                    {
                        reachedTarget = false;
                    }
                    else
                    {
                        reachedTarget = true;
                    }
                }
                else
                {
                    if (Mathf.Abs(transform.position.x - camTarget.x) < .1f)
                    {
                        reachedTarget = true;
                    }
                    else
                    {
                        reachedTarget = false;
                    }
                }

                if (reachedTarget)
                {
                    MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.y;
                }
            }
        }

        public void PlayGoingInsideAnimation(float time)
        {
            followActive = false;
            transform.DOMoveY(transform.position.y - 0.3f, time);
            camera.DOOrthoSize(startingOrthographicSize - 1, time);
        }

        public void PlayGoingInsideMachineAnimation(float time)
        {
            followActive = false;
            //transform.DOMoveY(transform.position.y - 0.3f, time);
            //camera.DOOrthoSize(startingOrthographicSize - 1, time);
        }
    }
}