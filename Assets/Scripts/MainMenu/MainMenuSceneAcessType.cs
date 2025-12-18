using UnityEngine;

public enum SceneAccessType
{
    Dev,
    Publish,
    Both
}

public class MainMenuSceneAcessType : MonoBehaviour
{
    [Header("Select scene access type for this session")]
    public SceneAccessType SceneAccessType;

    private void Awake()
    {
#if UNITY_EDITOR
        // Load last saved choice if key exists
        if (UnityEditor.EditorPrefs.HasKey("SelectedSceneAccessType"))
        {
            SceneAccessType = (SceneAccessType)UnityEditor.EditorPrefs.GetInt("SelectedSceneAccessType");
        }
#endif
        // Apply to SceneManagement
        SceneManagement.sceneAccessType = SceneAccessType;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Save current choice whenever changed in Inspector
        UnityEditor.EditorPrefs.SetInt("SelectedSceneAccessType", (int)SceneAccessType);
    }
#endif
}
