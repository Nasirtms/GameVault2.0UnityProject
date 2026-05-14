using UnityEngine;

public enum UrlType
{
    Local,
    Production
}

public enum SceneAccessType
{
    Dev,
    Publish,
    Both
}

public enum BuildMode
{
    Local,
    Production
}

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Build/Build Config")]
public class BuildConfig : ScriptableObject
{
    public UrlType urlType;
    public SceneAccessType gameType;
    public BuildMode buildMode;
    public bool isPlaySplashVideo;

    private const string localUrl = "http://localhost:5036";
    private const string publishUrl = "https://gamevault222.com";

    private const string LocalSocketUrl = "ws://localhost:5036";
    private const string ProductionSocketUrl = "wss://gamevault222.com";

    public string GetUrl()
    {
        return urlType == UrlType.Production ? publishUrl : localUrl;
    }

    public string GetSocketBaseUrl()
    {
        return urlType == UrlType.Production ? ProductionSocketUrl : LocalSocketUrl;
    }

    public bool IsProduction() => gameType == SceneAccessType.Publish;
    public bool IsDevelopment() => gameType == SceneAccessType.Dev;
    public bool IsBoth() => gameType == SceneAccessType.Both;
}