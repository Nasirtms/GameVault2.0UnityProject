//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEngine;

//[InitializeOnLoad]
//public static class BuildInterceptor
//{
//    static BuildInterceptor()
//    {
//        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildButtonPressed);
//    }

//    private static void OnBuildButtonPressed(BuildPlayerOptions buildOptions)
//    {
//        BuildOptionPopup.ShowPopup(buildOptions);
//    }
//}

//public class BuildOptionPopup : EditorWindow
//{
//    private static BuildPlayerOptions pendingBuildOptions;
//    private static string[] optionNames = { "Dev", "Publish", "Both" };
//    private static int selectedOption = 0;

//    public static void ShowPopup(BuildPlayerOptions buildOptions)
//    {
//        // Load last selected option if it exists
//        if (EditorPrefs.HasKey("SelectedSceneAccessType"))
//            selectedOption = EditorPrefs.GetInt("SelectedSceneAccessType");

//        pendingBuildOptions = buildOptions;
//        var window = CreateInstance<BuildOptionPopup>();
//        window.titleContent = new GUIContent("Select Scene Access Type");
//        window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);
//        window.ShowModalUtility();
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Choose build access type:", EditorStyles.boldLabel);

//        selectedOption = GUILayout.SelectionGrid(selectedOption, optionNames, 1);

//        GUILayout.Space(10);

//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("Cancel"))
//        {
//            Close();
//        }
//        if (GUILayout.Button("Build"))
//        {
//            // Save selected type so Awake() can use it
//            EditorPrefs.SetInt("SelectedSceneAccessType", selectedOption);

//            // Set in SceneManagement immediately for build-time usage
//            SceneManagement.sceneAccessType = (SceneAccessType)selectedOption;

//            Debug.Log($"✅ Build started with SceneAccessType: {(SceneAccessType)selectedOption}");

//            // Continue Unity build
//            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(pendingBuildOptions);
//            Close();
//        }
//        GUILayout.EndHorizontal();
//    }
//}
//#endif
