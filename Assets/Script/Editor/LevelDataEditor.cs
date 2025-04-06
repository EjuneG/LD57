using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private bool showFrameSets = true;
    private bool showFrameEvents = true;
    private bool showButtonConfigs = true;
    
    private SerializedProperty levelNameProp;
    private SerializedProperty levelDescriptionProp;
    private SerializedProperty initialFrameSetProp;
    private SerializedProperty frameSetsProp;
    private SerializedProperty frameEventsProp;
    private SerializedProperty buttonConfigsProp;
    
    private void OnEnable()
    {
        levelNameProp = serializedObject.FindProperty("levelName");
        levelDescriptionProp = serializedObject.FindProperty("levelDescription");
        initialFrameSetProp = serializedObject.FindProperty("initialFrameSet");
        frameSetsProp = serializedObject.FindProperty("frameSets");
        frameEventsProp = serializedObject.FindProperty("frameEvents");
        buttonConfigsProp = serializedObject.FindProperty("buttonConfigs");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        LevelData levelData = (LevelData)target;
        
        EditorGUILayout.PropertyField(levelNameProp);
        EditorGUILayout.PropertyField(levelDescriptionProp);
        
        EditorGUILayout.Space();
        
        // Frame Sets
        showFrameSets = EditorGUILayout.Foldout(showFrameSets, "Frame Sets", true);
        if (showFrameSets)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(initialFrameSetProp);
            
            EditorGUILayout.PropertyField(frameSetsProp, new GUIContent("Frame Sets"), true);
            
            // Button to auto-populate frame sets from Resources
            if (GUILayout.Button("Auto-Detect Frame Sets from Resources"))
            {
                AutoDetectFrameSets();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Frame Events
        showFrameEvents = EditorGUILayout.Foldout(showFrameEvents, "Frame Events", true);
        if (showFrameEvents)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(frameEventsProp, new GUIContent("Frame Events"), true);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Narration Event"))
            {
                AddFrameEvent(FrameEventType.PlayNarration);
            }
            
            if (GUILayout.Button("Add Switch Frame Set Event"))
            {
                AddFrameEvent(FrameEventType.SwitchFrameSet);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Button Configs
        showButtonConfigs = EditorGUILayout.Foldout(showButtonConfigs, "Button Configurations", true);
        if (showButtonConfigs)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(buttonConfigsProp, new GUIContent("Button Configs"), true);
            
            // Button to open Button Finder tool
            if (GUILayout.Button("Open Button Finder Tool"))
            {
                ButtonFinderWindow.ShowWindow();
                // Focus on window
                EditorWindow window = EditorWindow.GetWindow<ButtonFinderWindow>();
                if (window != null)
                {
                    // Set the target LevelData
                    var buttonFinderField = window.GetType().GetField("targetLevelData");
                    if (buttonFinderField != null)
                    {
                        buttonFinderField.SetValue(window, target);
                    }
                    window.Focus();
                }
            }
            
            // Help box for button configs
            EditorGUILayout.HelpBox(
                "Use the Button Finder Tool to scan your scene for FrameSensitiveButtons " +
                "and add them to this level data.\n\n" +
                "Each button config uses the Object ID to find the matching button in the scene.",
                MessageType.Info);
            
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void AddFrameEvent(FrameEventType eventType)
    {
        // Add a new frame event
        frameEventsProp.InsertArrayElementAtIndex(frameEventsProp.arraySize);
        SerializedProperty newEvent = frameEventsProp.GetArrayElementAtIndex(frameEventsProp.arraySize - 1);
        
        // Set default values
        newEvent.FindPropertyRelative("frameIndex").intValue = 0;
        newEvent.FindPropertyRelative("eventType").enumValueIndex = (int)eventType;
        newEvent.FindPropertyRelative("triggerOnce").boolValue = true;
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void AutoDetectFrameSets()
    {
        // Clear existing array
        frameSetsProp.ClearArray();
        
        // Look for directories in Resources folder
        string resourcesPath = Application.dataPath + "/Resources";
        if (Directory.Exists(resourcesPath))
        {
            string[] directories = Directory.GetDirectories(resourcesPath);
            int index = 0;
            
            foreach (string dir in directories)
            {
                string dirName = new DirectoryInfo(dir).Name;
                
                // Check if directory contains sprite files
                string[] files = Directory.GetFiles(dir, "*.png");
                if (files.Length > 0)
                {
                    // Add frame set
                    frameSetsProp.InsertArrayElementAtIndex(index);
                    var element = frameSetsProp.GetArrayElementAtIndex(index);
                    
                    element.FindPropertyRelative("setName").stringValue = dirName;
                    element.FindPropertyRelative("resourcePath").stringValue = dirName;
                    
                    index++;
                }
            }
            
            // Set initial frame set if we found at least one
            if (index > 0 && string.IsNullOrEmpty(initialFrameSetProp.stringValue))
            {
                var firstElement = frameSetsProp.GetArrayElementAtIndex(0);
                initialFrameSetProp.stringValue = firstElement.FindPropertyRelative("setName").stringValue;
            }
        }
        else
        {
            Debug.LogWarning("Resources folder not found!");
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif