using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class ButtonFinderWindow : EditorWindow
{
    private LevelData targetLevelData;
    private Vector2 scrollPosition;
    private List<FrameSensitiveButton> sceneButtons = new List<FrameSensitiveButton>();
    private Dictionary<string, bool> selectedButtons = new Dictionary<string, bool>();
    
    [MenuItem("Game Tools/Button Finder")]
    public static void ShowWindow()
    {
        GetWindow<ButtonFinderWindow>("Button Finder");
    }
    
    private void OnEnable()
    {
        RefreshSceneButtons();
    }
    
    private void OnGUI()
    {
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Button Finder Tool", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
        {
            RefreshSceneButtons();
        }
        EditorGUILayout.EndHorizontal();
        
        // Target LevelData selection
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Level Data", EditorStyles.boldLabel);
        targetLevelData = (LevelData)EditorGUILayout.ObjectField(targetLevelData, typeof(LevelData), false);
        
        if (targetLevelData == null)
        {
            EditorGUILayout.HelpBox("Please select a Level Data asset to add buttons to.", MessageType.Info);
        }
        
        // Scene buttons list
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Scene Buttons ({sceneButtons.Count})", EditorStyles.boldLabel);
        
        if (sceneButtons.Count == 0)
        {
            EditorGUILayout.HelpBox("No FrameSensitiveButtons found in the scene.", MessageType.Warning);
        }
        else
        {
            // Get existing IDs if we have a target
            HashSet<string> existingIds = new HashSet<string>();
            if (targetLevelData != null)
            {
                foreach (var config in targetLevelData.buttonConfigs)
                {
                    if (!string.IsNullOrEmpty(config.buttonId))
                    {
                        existingIds.Add(config.buttonId);
                    }
                }
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add", GUILayout.Width(30));
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Object ID", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            // Draw separator line
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Button rows
            foreach (var button in sceneButtons)
            {
                if (button == null) continue;
                
                string buttonId = button.ObjectId;
                bool exists = existingIds.Contains(buttonId);
                bool selected = selectedButtons.ContainsKey(buttonId) && selectedButtons[buttonId];
                
                // If button already exists, don't allow selection
                if (exists)
                {
                    selectedButtons[buttonId] = false;
                    selected = false;
                }
                
                EditorGUILayout.BeginHorizontal();
                
                // Selection checkbox
                bool newSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(30));
                if (newSelected != selected && !exists)
                {
                    selectedButtons[buttonId] = newSelected;
                }
                
                // Button name and ID
                EditorGUILayout.LabelField(button.name, GUILayout.Width(150));
                EditorGUILayout.LabelField(buttonId, GUILayout.Width(150));
                
                // Status of the button
                string status = exists ? "Already Added" : "New";
                Color originalColor = GUI.color;
                GUI.color = exists ? Color.green : Color.yellow;
                EditorGUILayout.LabelField(status, GUILayout.Width(100));
                GUI.color = originalColor;
                
                // Select in scene button
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = button.gameObject;
                    EditorGUIUtility.PingObject(button.gameObject);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // Action buttons
        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(targetLevelData == null);
        
        if (GUILayout.Button("Add Selected Buttons to Level Data"))
        {
            AddSelectedButtonsToLevelData();
        }
        
        if (GUILayout.Button("Select All New Buttons"))
        {
            SelectAllNewButtons(targetLevelData);
        }
        
        EditorGUI.EndDisabledGroup();
    }
    
    private void RefreshSceneButtons()
    {
        // Find all FrameSensitiveButtons (including inactive)
        sceneButtons = new List<FrameSensitiveButton>(FindObjectsOfType<FrameSensitiveButton>(true));
        
        // Sort by name
        sceneButtons.Sort((a, b) => string.Compare(a.name, b.name));
        
        // Initialize selection dictionary
        selectedButtons.Clear();
        foreach (var button in sceneButtons)
        {
            if (button != null)
            {
                selectedButtons[button.ObjectId] = false;
            }
        }
    }
    
    private void SelectAllNewButtons(LevelData levelData)
    {
        if (levelData == null) return;
        
        // Get existing IDs
        HashSet<string> existingIds = new HashSet<string>();
        foreach (var config in levelData.buttonConfigs)
        {
            if (!string.IsNullOrEmpty(config.buttonId))
            {
                existingIds.Add(config.buttonId);
            }
        }
        
        // Select all buttons that don't already exist
        foreach (var button in sceneButtons)
        {
            if (button == null) continue;
            
            string buttonId = button.ObjectId;
            if (!string.IsNullOrEmpty(buttonId) && !existingIds.Contains(buttonId))
            {
                selectedButtons[buttonId] = true;
            }
        }
    }
    
    private void AddSelectedButtonsToLevelData()
    {
        if (targetLevelData == null) return;
        
        // Create a dictionary for quick lookup
        Dictionary<string, FrameSensitiveButton> buttonLookup = new Dictionary<string, FrameSensitiveButton>();
        foreach (var button in sceneButtons)
        {
            if (button != null && !string.IsNullOrEmpty(button.ObjectId))
            {
                buttonLookup[button.ObjectId] = button;
            }
        }
        
        // Count selected buttons
        int addCount = 0;
        foreach (var entry in selectedButtons)
        {
            if (entry.Value && buttonLookup.ContainsKey(entry.Key))
            {
                addCount++;
            }
        }
        
        if (addCount == 0)
        {
            EditorUtility.DisplayDialog("No Buttons Selected", 
                "Please select at least one button to add to the level data.", 
                "OK");
            return;
        }
        
        // Record undo
        Undo.RecordObject(targetLevelData, "Add Buttons to Level Data");
        
        // Add each selected button
        foreach (var entry in selectedButtons)
        {
            if (entry.Value && buttonLookup.TryGetValue(entry.Key, out FrameSensitiveButton button))
            {
                // Create new button config
                ButtonConfig newConfig = new ButtonConfig
                {
                    buttonId = entry.Key,
                    displayName = button.name,
                    actionType = InteractionActionType.PlayNarration  // Default action
                };
                
                // Add to level data
                targetLevelData.buttonConfigs.Add(newConfig);
                
                // Reset selection
                selectedButtons[entry.Key] = false;
            }
        }
        
        // Mark dirty
        EditorUtility.SetDirty(targetLevelData);
        
        // Show confirmation
        EditorUtility.DisplayDialog("Buttons Added", 
            $"Added {addCount} button{(addCount > 1 ? "s" : "")} to level data.", 
            "OK");
    }
}
#endif