#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromFirstScene
{
    private const string PreviousSceneKey = "PlayFromFirstSceneEditor.PreviousScenePath";
    private const string LogoutFlag = "ForceLogoutInProgress";

    static PlayFromFirstScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // ?? Skip ALL logic if logout handler is pausing play mode
        if (EditorPrefs.GetBool(LogoutFlag, false))
            return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string firstScenePath = EditorBuildSettings.scenes[0].path;
            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            if (currentScenePath != firstScenePath)
            {
                EditorPrefs.SetString(PreviousSceneKey, currentScenePath);

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(firstScenePath);
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (EditorPrefs.HasKey(PreviousSceneKey))
            {
                string previousScenePath = EditorPrefs.GetString(PreviousSceneKey);

                if (!string.IsNullOrEmpty(previousScenePath) &&
                    previousScenePath != EditorSceneManager.GetActiveScene().path)
                {
                    EditorSceneManager.OpenScene(previousScenePath);
                }

                EditorPrefs.DeleteKey(PreviousSceneKey);
            }
        }
    }
}
#endif
