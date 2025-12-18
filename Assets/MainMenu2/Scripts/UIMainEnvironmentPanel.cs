using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class UIMainEnvironmentPanel : UIPanel
    {
        public Button leftButton;
        public Button rightButton;
        public Button backToCategoriesEnvButton;

        private void Start()
        {
            leftButton.onClick.AddListener(MainMenuManager.instance.LeftButtonPressed_Environment);
            rightButton.onClick.AddListener(MainMenuManager.instance.RightButtonPressed_Environment);
            backToCategoriesEnvButton.onClick.AddListener(MainMenuManager.instance.ExitCategoryBuilding);
        }
    }
}