using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class MenuGameMachine : MonoBehaviour
    {
        public string gameName;
        public GameObject machineObject;
        public BoxCollider2D collider;
        public SpriteRenderer machineSprite;
        public Transform entryPoint;

        public float machineSpriteWidth;

        public string gameID;
        public string sceneName;
        public string addressableLabel;

        private void OnValidate()
        {
            SetWidth();
            SetCollider();
        }

        void SetWidth()
        {
            if (machineSprite != null)
            {
                machineSpriteWidth = machineSprite.size.x * machineSprite.transform.lossyScale.x;
            }
        }

        void SetCollider()
        {
            if (machineSprite != null || collider == null)
            {
                collider.size = new Vector2(machineSprite.size.x * machineSprite.transform.lossyScale.x, machineSprite.size.y * machineSprite.transform.lossyScale.y);
                collider.offset = new Vector2(0, collider.size.y / 2);
            }
        }

        public void OnClicked()
        {

        }
    }
}