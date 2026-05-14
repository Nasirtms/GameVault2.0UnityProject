#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class BuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var config = Resources.Load<BuildConfig>("BuildConfig");

        if (config == null)
            throw new BuildFailedException("BuildConfig not found!");

        // =========================
        // DEFINE HANDLING
        // =========================
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(report.summary.platformGroup);
        var list = new System.Collections.Generic.List<string>(defines.Split(';'));
        list.RemoveAll(string.IsNullOrEmpty);

        if (config.IsProduction())
            list.Remove("DEVELOPMENT");
        else if (!list.Contains("DEVELOPMENT"))
            list.Add("DEVELOPMENT");

        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            report.summary.platformGroup,
            string.Join(";", list)
        );

        // =========================
        // AUTO CONFIG
        // =========================
        if (config.buildMode == BuildMode.Local)
        {
            config.urlType = UrlType.Local;
            config.gameType = SceneAccessType.Dev;
            config.isPlaySplashVideo = false;
        }
        else if (config.buildMode == BuildMode.Production)
        {
            config.urlType = UrlType.Production;
            config.gameType = SceneAccessType.Publish;
            config.isPlaySplashVideo = true;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Build validation passed ({config.buildMode})");
    }
}
#endif