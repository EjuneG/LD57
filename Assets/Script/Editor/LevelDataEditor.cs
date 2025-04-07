using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private bool showFrameSets = true;
    private bool showFrameEvents = true;
    private bool showButtonConfigs = true;
    private bool showAudioSettings = true;
    
    private SerializedProperty levelNameProp;
    private SerializedProperty levelDescriptionProp;
    private SerializedProperty transitionColorProp; // Added property for transition color
    private SerializedProperty dragDirection;
    private SerializedProperty backgroundMusic;
    private SerializedProperty initialFrameSetProp;
    private SerializedProperty frameSetsProp;
    private SerializedProperty frameEventsProp;
    private SerializedProperty buttonConfigsProp;
    
    // For BGM dropdown
    private string[] availableBGMs = new string[0];
    private int selectedBGMIndex = -1;
    
    private void OnEnable()
    {
        levelNameProp = serializedObject.FindProperty("levelName");
        levelDescriptionProp = serializedObject.FindProperty("levelDescription");
        transitionColorProp = serializedObject.FindProperty("transitionColor"); // Initialize the transition color property
        dragDirection = serializedObject.FindProperty("dragDirection");
        backgroundMusic = serializedObject.FindProperty("backgroundMusic");
        initialFrameSetProp = serializedObject.FindProperty("initialFrameSet");
        frameSetsProp = serializedObject.FindProperty("frameSets");
        frameEventsProp = serializedObject.FindProperty("frameEvents");
        buttonConfigsProp = serializedObject.FindProperty("buttonConfigs");
        
        // Get available BGMs
        RefreshAvailableBGMs();
    }
    
    private void RefreshAvailableBGMs()
    {
        // Find available BGMs from Resources/Audio folder
        List<string> bgmList = new List<string>();
        bgmList.Add("(None)"); // First option is none/empty
        
        // Check if Resources/Audio exists
        string audioPath = Application.dataPath + "/Resources/Audio";
        if (Directory.Exists(audioPath))
        {
            string[] audioFiles = Directory.GetFiles(audioPath, "*.wav");
            foreach (string file in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bgmList.Add(fileName);
            }
            
            // Also check MP3 files
            audioFiles = Directory.GetFiles(audioPath, "*.mp3");
            foreach (string file in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bgmList.Add(fileName);
            }
            
            // Also check OGG files
            audioFiles = Directory.GetFiles(audioPath, "*.ogg");
            foreach (string file in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bgmList.Add(fileName);
            }
        }
        
        availableBGMs = bgmList.ToArray();
        
        // Set selected index based on current value
        selectedBGMIndex = 0; // Default to "None"
        string currentBGM = backgroundMusic.stringValue;
        
        if (!string.IsNullOrEmpty(currentBGM))
        {
            for (int i = 1; i < availableBGMs.Length; i++)
            {
                if (availableBGMs[i] == currentBGM)
                {
                    selectedBGMIndex = i;
                    break;
                }
            }
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        LevelData levelData = (LevelData)target;
        
        // Level Information section
        EditorGUILayout.LabelField("Level Information", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelNameProp);
        EditorGUILayout.PropertyField(levelDescriptionProp);
        
        // Add transition color property with tooltip
        EditorGUILayout.PropertyField(transitionColorProp, new GUIContent("Transition Color", 
            "The color tint to use when transitioning to this level (Default=Black, Green=Success, Red=Failure)"));
        
        EditorGUILayout.Space();
        
        // FOV Control Settings
        EditorGUILayout.LabelField("FOV Control Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dragDirection, new GUIContent("Choose Drag Direction", 
            "When checked, dragging down will move forward through frames (useful for levels where focus goes from top to bottom)"));
        
        EditorGUILayout.Space();
        
        // Audio Settings
        showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio Settings", true);
        if (showAudioSettings)
        {
            EditorGUI.indentLevel++;
            
            // Display dropdown for BGM selection
            if (availableBGMs.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("Background Music", 
                    "The BGM track to play when this level loads. Leave empty to keep current music."));
                
                int newIndex = EditorGUILayout.Popup(selectedBGMIndex, availableBGMs);
                if (newIndex != selectedBGMIndex)
                {
                    selectedBGMIndex = newIndex;
                    // Update the property
                    if (selectedBGMIndex == 0) // "None" option
                    {
                        backgroundMusic.stringValue = "";
                    }
                    else
                    {
                        backgroundMusic.stringValue = availableBGMs[selectedBGMIndex];
                    }
                }
                
                // Add refresh button
                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    RefreshAvailableBGMs();
                }
                EditorGUILayout.EndHorizontal();
                
                // Alternative: Direct text field input
                EditorGUILayout.PropertyField(backgroundMusic, new GUIContent("Custom Music Name", 
                    "You can also directly type the name of a music track here"));
            }
            else
            {
                // If no BGMs found, just show the regular field
                EditorGUILayout.PropertyField(backgroundMusic, new GUIContent("Background Music", 
                    "The name of the BGM track to play when this level loads. Leave empty to keep current music."));
                
                EditorGUILayout.HelpBox(
                    "No audio files found in Resources/Audio folder.\nCreate this folder and add audio files to enable the dropdown menu.",
                    MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
        }
        
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