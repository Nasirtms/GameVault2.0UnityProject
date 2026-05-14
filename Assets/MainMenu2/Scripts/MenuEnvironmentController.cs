using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MainMenu
{
    public class MenuEnvironmentController : MonoBehaviour
    {
        public MenuEnvironmentDatabase menuEnvDatabase;

        public MenuBuildingEnvironment buildingEnvironment;
        public List<MenuCategoryEnvironment> categoryEnvironments;

        public int currentCategoryEnvIndex = -1;

        public MenuCategoryEnvironment currentActiveCategoryEnvironment;

        public void EnableCategoryEnvironment(eGameCategories categoryName)
        {
            MenuCategoryEnvironment currentCategotyEnv = categoryEnvironments.FirstOrDefault(x=>x.categoryName == categoryName);
            if (currentCategotyEnv != null)
            {
                currentCategoryEnvIndex = categoryEnvironments.IndexOf(currentCategotyEnv);
                categoryEnvironments[currentCategoryEnvIndex].gameObject.SetActive(true);
                currentActiveCategoryEnvironment = categoryEnvironments[currentCategoryEnvIndex];
            }
        }

        public void DisableCurrentCategoryEnvironment()
        {
            if (currentCategoryEnvIndex > -1)
            {
                categoryEnvironments[currentCategoryEnvIndex].gameObject.SetActive(false);
                currentCategoryEnvIndex = -1;
                currentActiveCategoryEnvironment = null;
            }
        }

        public MenuCategoryEnvironment GetCategoryEnvironment(eGameCategories categoryName)
        {
            return categoryEnvironments.FirstOrDefault(x => x.categoryName == categoryName);
        }

        public void ResetFavoritesMachines()
        {
            ResetMachinesList(eGameCategories.Favorites);
        }

        void ResetMachinesList(eGameCategories categoryName)
        {
            MenuCategoryEnvironment cat = categoryEnvironments.FirstOrDefault(x => x.categoryName == categoryName);
            if (cat != null)
            {
                ResetMachinesList(cat);
            }
        }

        void ResetMachinesList(MenuCategoryEnvironment categoryEnvironment)
        {
            categoryEnvironment.ResetMachinesList();
        }
    }
}