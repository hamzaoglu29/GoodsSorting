#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GoodsSorting.Levels;

namespace GoodsSorting.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private bool showGridEditor = false;
        private bool showBackGridEditor = false;
        private GUIStyle cellStyle;
        private GUIStyle headerStyle;
        private Color[] itemColors = new Color[] {
            Color.red,                    // 0: Apple
            Color.yellow,                 // 1: Banana
            Color.green,                  // 2: Lime
            new Color(1f, 0.5f, 0f),      // 3: Orange
            new Color(0.5f, 0f, 0.5f),    // 4: Grape
            new Color(0f, 0.5f, 1f),      // 5: Blueberry
            Color.white                   // Empty (-1)
        };
        
        private void InitStyles()
        {
            if (cellStyle == null)
            {
                cellStyle = new GUIStyle(GUI.skin.button);
                cellStyle.padding = new RectOffset(0, 0, 0, 0);
                cellStyle.margin = new RectOffset(0, 0, 0, 0);
                cellStyle.alignment = TextAnchor.MiddleCenter;
                cellStyle.fontSize = 9;
            }
            
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.alignment = TextAnchor.MiddleCenter;
            }
        }
        
        public override void OnInspectorGUI()
        {
            InitStyles();
            
            LevelData levelData = (LevelData)target;
            
            // Draw default inspector for basic fields 
            DrawDefaultInspector();
            
            // Add button to open grid editor
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(showGridEditor ? "Hide Front Grid Editor" : "Show Front Grid Editor"))
            {
                showGridEditor = !showGridEditor;
            }
            
            if (GUILayout.Button(showBackGridEditor ? "Hide Back Grid Editor" : "Show Back Grid Editor"))
            {
                showBackGridEditor = !showBackGridEditor;
            }
            EditorGUILayout.EndHorizontal();
            
            if (showGridEditor)
            {
                DrawGridEditor(levelData, false);
                
                // Show item count warnings after the grid editor
                ShowItemMatchWarnings(levelData);
            }
            
            if (showBackGridEditor)
            {
                DrawGridEditor(levelData, true);
                
                // Show item count warnings after the grid editor
                ShowItemMatchWarnings(levelData);
            }
        }
        
        // Add a method to check and display warnings for item counts
        private void ShowItemMatchWarnings(LevelData levelData)
        {
            // Count all item types in both grids
            Dictionary<int, int> itemCounts = CountItemsInGrids(levelData);
            
            // Check if each item type has a count that's a multiple of 3
            bool hasWarning = false;
            string warningMessage = "Warning: The following items don't have counts divisible by 3 (needed for matches):\n";
            
            for (int type = 0; type < 6; type++) // 6 item types (0-5)
            {
                if (itemCounts.ContainsKey(type) && itemCounts[type] > 0)
                {
                    int remainder = itemCounts[type] % 3;
                    if (remainder != 0)
                    {
                        hasWarning = true;
                        string itemName = GetItemNameByType(type);
                        warningMessage += $"- {itemName}: {itemCounts[type]} items (need {3 - remainder} more for complete matches)\n";
                    }
                }
            }
            
            // Display warning if needed
            if (hasWarning)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
            }
            else if (itemCounts.Count > 0)
            {
                // Show a success message if we have items and they're all divisible by 3
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("All items have counts divisible by 3. Good job!", MessageType.Info);
            }
        }
        
        // Helper method to count items in both grids
        private Dictionary<int, int> CountItemsInGrids(LevelData levelData)
        {
            Dictionary<int, int> itemCounts = new Dictionary<int, int>();
            
            // Initialize counts for all item types
            for (int type = 0; type < 6; type++)
            {
                itemCounts[type] = 0;
            }
            
            // Count front grid items
            if (levelData.GridLayout != null)
            {
                foreach (int itemType in levelData.GridLayout)
                {
                    if (itemType >= 0 && itemType < 6) // Valid item type (0-5)
                    {
                        itemCounts[itemType]++;
                    }
                }
            }
            
            // Count back grid items
            if (levelData.BackGridLayout != null)
            {
                foreach (int itemType in levelData.BackGridLayout)
                {
                    if (itemType >= 0 && itemType < 6) // Valid item type (0-5)
                    {
                        itemCounts[itemType]++;
                    }
                }
            }
            
            return itemCounts;
        }
        
        // Helper method to get item name by type
        private string GetItemNameByType(int type)
        {
            switch (type)
            {
                case 0: return "Red (Apple)";
                case 1: return "Yellow (Banana)";
                case 2: return "Green (Lime)";
                case 3: return "Orange";
                case 4: return "Purple (Grape)";
                case 5: return "Blue (Blueberry)";
                default: return $"Unknown Type {type}";
            }
        }
        
        private void DrawGridEditor(LevelData levelData, bool isBackGrid)
        {
            string editorType = isBackGrid ? "Back" : "Front";
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{editorType} Grid Layout Editor", EditorStyles.boldLabel);
            
            // Get the correct property for this grid
            SerializedProperty gridLayoutProp = serializedObject.FindProperty(isBackGrid ? "_backGridLayout" : "_gridLayout");
            SerializedProperty frontGridRefProp = serializedObject.FindProperty(isBackGrid ? "_gridLayout" : "_backGridLayout");
            
            // Header with dimension controls
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Grid Dimensions:", GUILayout.Width(100));
            
            EditorGUI.BeginChangeCheck();
            SerializedProperty widthProp = serializedObject.FindProperty("GridWidth");
            SerializedProperty heightProp = serializedObject.FindProperty("GridHeight");
            SerializedProperty sectionsProp = serializedObject.FindProperty("NumberOfSections");
            
            int newWidth = EditorGUILayout.IntField("Width:", widthProp.intValue, GUILayout.Width(70));
            int newHeight = EditorGUILayout.IntField("Height:", heightProp.intValue, GUILayout.Width(70));
            int newSections = EditorGUILayout.IntField("Sections:", sectionsProp.intValue, GUILayout.Width(70));
            
            if (EditorGUI.EndChangeCheck())
            {
                // Apply changes
                widthProp.intValue = newWidth;
                heightProp.intValue = newHeight;
                sectionsProp.intValue = newSections;
                serializedObject.ApplyModifiedProperties();
                
                // This will trigger OnValidate in LevelData to adjust values if needed
                EditorUtility.SetDirty(levelData);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Ensure the grid layout exists
            if (gridLayoutProp.arraySize != levelData.GridWidth * levelData.GridHeight)
            {
                EditorGUILayout.HelpBox($"The {editorType.ToLower()} grid layout is not initialized or has incorrect dimensions. Click 'Initialize Grid' to create a default layout.", MessageType.Warning);
                
                if (GUILayout.Button($"Initialize {editorType} Grid"))
                {
                    levelData.EnsureGridLayoutExists();
                    EditorUtility.SetDirty(levelData);
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"{editorType} Layer: Click cells to cycle through item types. -1 = Empty", MessageType.Info);
                
                if (GUILayout.Button($"Clear {editorType} Grid", GUILayout.Width(120)))
                {
                    // Clear the grid
                    for (int i = 0; i < gridLayoutProp.arraySize; i++)
                    {
                        gridLayoutProp.GetArrayElementAtIndex(i).intValue = -1;
                    }
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(levelData);
                }
                EditorGUILayout.EndHorizontal();
                
                // Draw column headers
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20); // Space for row headers
                for (int x = 0; x < levelData.GridWidth; x++)
                {
                    // Get the section for this column
                    int section = x / (levelData.GridWidth / levelData.NumberOfSections);
                    bool inDisabledSection = levelData.DisabledSections != null && 
                                           System.Array.IndexOf(levelData.DisabledSections, section) >= 0;
                    
                    // Show section separators
                    int columnsPerSection = levelData.GridWidth / levelData.NumberOfSections;
                    if (x > 0 && x % columnsPerSection == 0)
                    {
                        GUILayout.Label("│", headerStyle, GUILayout.Width(15));
                    }
                    
                    // Color the header for disabled sections
                    GUI.color = inDisabledSection ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
                    GUILayout.Label(x.ToString(), headerStyle, GUILayout.Width(30));
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();
                
                // Draw the grid editor
                for (int y = levelData.GridHeight - 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Draw row header
                    GUILayout.Label(y.ToString(), headerStyle, GUILayout.Width(20));
                    
                    // Draw each cell in this row
                    for (int x = 0; x < levelData.GridWidth; x++)
                    {
                        // Check if this cell is in a disabled section
                        int section = x / (levelData.GridWidth / levelData.NumberOfSections);
                        bool inDisabledSection = levelData.DisabledSections != null && 
                                               System.Array.IndexOf(levelData.DisabledSections, section) >= 0;
                        
                        // Show section separators
                        int columnsPerSection = levelData.GridWidth / levelData.NumberOfSections;
                        if (x > 0 && x % columnsPerSection == 0)
                        {
                            GUILayout.Label("│", headerStyle, GUILayout.Width(15));
                        }
                        
                        int index = y * levelData.GridWidth + x;
                        int itemType = gridLayoutProp.GetArrayElementAtIndex(index).intValue;
                        int frontItemType = -2; // Default to something that's not -1
                        
                        // If this is the back grid, check if there's a front item at this position
                        if (isBackGrid && frontGridRefProp.arraySize > index)
                        {
                            frontItemType = frontGridRefProp.GetArrayElementAtIndex(index).intValue;
                        }
                        
                        // For back grid, prepare cell text
                        string cellText = itemType.ToString();
                        if (isBackGrid && frontItemType != -1)
                        {
                            // Can't see back item because front item is present
                            cellText = $"{itemType}*";
                        }
                        
                        // In disabled sections, don't allow clicks
                        if (inDisabledSection)
                        {
                            GUI.enabled = false;
                            GUI.backgroundColor = Color.gray;
                            GUILayout.Box(cellText, cellStyle, GUILayout.Width(30), GUILayout.Height(30));
                            GUI.backgroundColor = Color.white;
                            GUI.enabled = true;
                        }
                        else
                        {
                            // Create a rect for our custom button handling
                            Rect buttonRect = GUILayoutUtility.GetRect(30, 30);
                            
                            // Handle right click before button rendering
                            Event currentEvent = Event.current;
                            bool isRightClick = currentEvent.type == EventType.MouseDown && 
                                              currentEvent.button == 1 && 
                                              buttonRect.Contains(currentEvent.mousePosition);
                            
                            if (isRightClick)
                            {
                                // Right click - cycle backward
                                int newType = itemType - 1;
                                if (newType < -1) newType = 5; // Cycle to highest item type
                                
                                gridLayoutProp.GetArrayElementAtIndex(index).intValue = newType;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(levelData);
                                
                                // Consume the event to prevent further processing
                                currentEvent.Use();
                                // Force repaint to update UI immediately
                                Repaint();
                                // Skip rendering the button since we've handled the event
                                continue;
                            }
                            
                            // Set text color
                            Color originalTextColor = GUI.skin.button.normal.textColor;
                            if (isBackGrid && frontItemType != -1)
                            {
                                // Can't see back item because front item is present
                                GUI.contentColor = Color.gray;
                            }
                            else
                            {
                                GUI.contentColor = Color.black;
                            }
                            
                            // Draw the button visually
                            GUI.backgroundColor = GetColorForItemType(itemType);
                            
                            // Create a standard button with simpler left-click handling
                            if (GUI.Button(buttonRect, cellText, cellStyle))
                            {
                                // Left click - cycle forward
                                int newType = itemType + 1;
                                if (newType > 5) newType = -1; // Cycle back to empty
                                
                                gridLayoutProp.GetArrayElementAtIndex(index).intValue = newType;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(levelData);
                            }
                            
                            // Reset colors
                            GUI.backgroundColor = Color.white;
                            GUI.contentColor = originalTextColor;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                // Draw section labels below the grid
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30); // Offset to align with grid
                
                int sectionWidth = levelData.GridWidth / levelData.NumberOfSections * 30; // 30 is the width of each cell
                
                for (int section = 0; section < levelData.NumberOfSections; section++)
                {
                    bool isDisabled = levelData.DisabledSections != null && 
                                     System.Array.IndexOf(levelData.DisabledSections, section) >= 0;
                    
                    GUI.color = isDisabled ? Color.gray : Color.black;
                    GUILayout.Label($"Section {section}", GUILayout.Width(sectionWidth));
                    GUI.color = Color.white;
                    
                    // Add separator space
                    if (section < levelData.NumberOfSections - 1)
                    {
                        GUILayout.Space(15);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        // Add helper method to get the color for an item type
        private Color GetColorForItemType(int itemType)
        {
            // Default to gray for empty
            if (itemType < 0 || itemType >= itemColors.Length - 1)
            {
                return Color.gray;
            }
            
            return itemColors[itemType];
        }
    }
}
#endif 