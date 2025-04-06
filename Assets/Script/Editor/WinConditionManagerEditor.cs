#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WinConditionManager))]
public class WinConditionManagerEditor : Editor
{
    private SerializedProperty levelBranchesProp;
    private bool showHelpBox = true;
    
    private void OnEnable()
    {
        levelBranchesProp = serializedObject.FindProperty("levelBranches");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Help box
        if (showHelpBox)
        {
            EditorGUILayout.HelpBox(
                "Configure level branches to determine where the player goes based on their choices.\n\n" +
                "For each level, specify the next level based on whether the player made a green flag (correct) " +
                "or red flag (incorrect) choice.", 
                MessageType.Info);
            
            if (GUILayout.Button("Hide Help"))
            {
                showHelpBox = false;
            }
        }
        else if (GUILayout.Button("Show Help"))
        {
            showHelpBox = true;
        }
        
        EditorGUILayout.Space();
        
        // Draw default inspector excluding level branches
        DrawPropertiesExcluding(serializedObject, "levelBranches");
        
        EditorGUILayout.Space();
        
        // Level branches header
        EditorGUILayout.LabelField("Level Branches", EditorStyles.boldLabel);
        
        // Add button at the top
        if (GUILayout.Button("Add Level Branch"))
        {
            levelBranchesProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }
        
        // Draw each level branch
        for (int i = 0; i < levelBranchesProp.arraySize; i++)
        {
            SerializedProperty branchProp = levelBranchesProp.GetArrayElementAtIndex(i);
            SerializedProperty levelNameProp = branchProp.FindPropertyRelative("levelName");
            SerializedProperty nextGreenProp = branchProp.FindPropertyRelative("nextLevelIfGreenFlag");
            SerializedProperty nextRedProp = branchProp.FindPropertyRelative("nextLevelIfRedFlag");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header with delete button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Branch {i + 1}: {levelNameProp.stringValue}", EditorStyles.boldLabel);
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                levelBranchesProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();
            
            // Branch properties
            EditorGUILayout.PropertyField(levelNameProp, new GUIContent("Level Name"));
            EditorGUILayout.PropertyField(nextGreenProp, new GUIContent("Next Level (Green Flag)"));
            EditorGUILayout.PropertyField(nextRedProp, new GUIContent("Next Level (Red Flag)"));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif