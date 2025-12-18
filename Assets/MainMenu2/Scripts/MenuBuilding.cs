using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class MenuBuilding : MonoBehaviour
    {
        public eGameCategories categoryName;
        public GameObject buildingObject;
        public SpriteRenderer buildingSprite;
        public Transform entryPoint;

        public float buildingWidth;

        private void OnValidate()
        {
            SetWidth();
        }

        void SetWidth()
        {
            if (buildingSprite != null)
            {
                buildingWidth = buildingSprite.size.x * buildingSprite.transform.lossyScale.x;
            }
        }

        public void OnClicked()
        {

        }

        //private void OnMouseDown()
        //{
        //    Debug.Log(gameObject.name + " clicked");
        //}
    }
}