using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class MoveTargetMarker : MonoBehaviour
    {
        public bool followActive = true;
        public Transform followTarget;
        //public float followSpeed = 1;
        //public Vector3 offset = new Vector3(0f, -1.41f, 0f);

        private Vector3 followPosition;

        private void Start()
        {
            followPosition = transform.position;
        }

        private void Update()
        {
            if (followActive)
            {
                if (followTarget == null)
                    return;

                followPosition.x = followTarget.position.x;
                transform.position = followPosition;
            }
        }
    }
}