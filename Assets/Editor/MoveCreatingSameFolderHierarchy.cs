using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class MoveCreatingSameFolderHierarchy
{
    [MenuItem("Assets/Custom/Move Creating Same Folder Hierarchy", false, 1000)]
    public static void MoveAndCreateFolderHierarchy()
    {
        foreach (Object item in Selection.objects)
        {
            string sourcePath = AssetDatabase.GetAssetPath(item);
            Debug.Log(sourcePath);
        }
    }
}
