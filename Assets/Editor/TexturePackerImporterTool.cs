using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using SimpleJSON; // Use a JSON lib like SimpleJSON (or Unity's JsonUtility if structure is known)

public class TexturePackerImporterTool : EditorWindow
{
    private Texture2D texture;
    private TextAsset jsonFile;
    private bool generateAnimation = false;
    private string animationClipName = "NewAnimation";
    private float frameRate = 12f;

    [MenuItem("Tools/TexturePacker Importer")]
    public static void ShowWindow()
    {
        GetWindow<TexturePackerImporterTool>("TexturePacker Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("TexturePacker Import Settings", EditorStyles.boldLabel);
        texture = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", texture, typeof(Texture2D), false);
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        generateAnimation = EditorGUILayout.Toggle("Generate Animation", generateAnimation);

        if (generateAnimation)
        {
            animationClipName = EditorGUILayout.TextField("Animation Name", animationClipName);
            frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);
        }

        if (GUILayout.Button("Process"))
        {
            Process();
        }
    }

    private void Process()
    {
        if (texture == null || jsonFile == null)
        {
            Debug.LogError("Missing texture or JSON.");
            return;
        }

        string texturePath = AssetDatabase.GetAssetPath(texture);
        string jsonText = jsonFile.text;
        JSONNode root = JSON.Parse(jsonText);
        var frames = root["frames"].AsObject;

        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        importer.spriteImportMode = SpriteImportMode.Multiple;

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        foreach (KeyValuePair<string, JSONNode> kv in frames)
        {
            var frame = kv.Value["frame"];
            var meta = new SpriteMetaData
            {
                name = kv.Key,
                rect = new Rect(
                    frame["x"].AsInt,
                    texture.height - frame["y"].AsInt - frame["h"].AsInt,
                    frame["w"].AsInt,
                    frame["h"].AsInt
                ),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = 9
            };
            metas.Add(meta);
        }

        importer.spritesheet = metas.ToArray();
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

        if (generateAnimation)
        {
            GenerateAnimation(texturePath, animationClipName, frameRate);
        }

        Debug.Log("Imported and sliced successfully.");
    }

    private void GenerateAnimation(string texturePath, string clipName, float frameRate)
    {
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
        List<Sprite> spriteList = new List<Sprite>();

        foreach (var s in sprites)
        {
            if (s is Sprite sprite)
                spriteList.Add(sprite);
        }

        spriteList.Sort((a, b) => a.name.CompareTo(b.name)); // Order by name like 0001, 0002, etc.

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;

        // Bind to UI Image.sprite instead of SpriteRenderer
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(UnityEngine.UI.Image),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[spriteList.Count];
        for (int i = 0; i < spriteList.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = spriteList[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

        string clipPath = Path.GetDirectoryName(texturePath) + "/" + clipName + ".anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"🎞️ Animation clip '{clipName}' created for UI Image.");
    }

}
