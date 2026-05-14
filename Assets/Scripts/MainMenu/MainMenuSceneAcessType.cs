using Unity.VisualScripting.FullSerializer;
using UnityEngine;


public class MainMenuSceneAcessType : MonoBehaviour
{
    private void Awake()
    {
        if (SceneManagement.buildConfig == null)
        {
            Debug.LogError("BuildConfig is NULL!");
            return;
        }

        SceneManagement.sceneAccessType = SceneManagement.buildConfig.gameType;
    }
}
