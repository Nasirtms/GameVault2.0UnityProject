using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FishDatabase))]
public class FishDatabaseEditor : Editor
{
    private int tabIndex = 0;
    private readonly string[] tabNames = { "Normal Fishes", "Bonus Fishes" };

    private SerializedProperty fishListProp;
    private SerializedProperty bonusFishesProp;

    private void OnEnable()
    {
        fishListProp = serializedObject.FindProperty("fishList");
        bonusFishesProp = serializedObject.FindProperty("bonusFishes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        tabIndex = GUILayout.Toolbar(tabIndex, tabNames);
        GUILayout.Space(10);

        if (tabIndex == 0)
        {
            EditorGUILayout.PropertyField(fishListProp, new GUIContent("Normal Fishes"), true);
        }
        else if (tabIndex == 1)
        {
            EditorGUILayout.LabelField("Bonus Fish Sets", EditorStyles.boldLabel);
            GUILayout.Space(5);

            for (int i = 0; i < bonusFishesProp.arraySize; i++)
            {
                SerializedProperty setProp = bonusFishesProp.GetArrayElementAtIndex(i);

                SerializedProperty speedProp = setProp.FindPropertyRelative("bonusFishSpeed");
                SerializedProperty movementTypeProp = setProp.FindPropertyRelative("movementType");
                SerializedProperty spawnOffsetProp = setProp.FindPropertyRelative("spawnOffset");

                SerializedProperty bigFishesProp = setProp.FindPropertyRelative("bigFishes");
                SerializedProperty patternGroupsProp = setProp.FindPropertyRelative("patternFishGroups");

                EditorGUILayout.BeginVertical("box");

                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Set {i + 1}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    bonusFishesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                EditorGUILayout.PropertyField(speedProp, new GUIContent("Bonus Fish Speed"));
                EditorGUILayout.PropertyField(movementTypeProp, new GUIContent("Movement Type"));
                EditorGUILayout.PropertyField(spawnOffsetProp, new GUIContent("Spawn Offset"));

                EditorGUILayout.PropertyField(bigFishesProp, new GUIContent("Big Fishes"), true);

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Pattern Fish Groups", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(patternGroupsProp, true);

                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            if (GUILayout.Button("Add New Bonus Fish Set"))
            {
                bonusFishesProp.InsertArrayElementAtIndex(bonusFishesProp.arraySize);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
