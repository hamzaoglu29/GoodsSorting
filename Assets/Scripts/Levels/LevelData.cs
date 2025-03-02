using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoodsSorting.Levels
{
    [Serializable]
    public class SortingGoal
    {
        public int ItemType;
        public int TargetCount;
    }
    
    [CreateAssetMenu(fileName = "LevelData", menuName = "Goods Sorting/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int LevelNumber;
        public string LevelName;
        
        [Header("Level Goals")]
        public List<SortingGoal> SortingGoals = new List<SortingGoal>();
        
        [Header("Level Constraints")]
        public int MovesLimit = 0; // 0 = unlimited
        public float TimeLimit = 0; // 0 = unlimited
        
        [Header("Grid Settings")]
        public int GridWidth = 9;
        public int GridHeight = 4;
        public int NumberOfSections = 3;  // Added to allow customizing section count
        
        [Header("Grid Layout")]
        [Tooltip("Predefined grid layout. Each integer represents an item type (0-5). Use -1 for empty cells.")]
        [SerializeField] private int[] _gridLayout;
        
        [Header("Back Layer Grid")]
        [Tooltip("The back layer grid layout. Each integer represents an item type (0-5). Use -1 for empty cells.")]
        [SerializeField] private int[] _backGridLayout;
        
        [Header("Section Settings")]
        [Tooltip("Array of section indices that should be disabled (not drawn or processed)")]
        [SerializeField] private int[] _disabledSections = new int[0];
        
        // Properties to access the grid layouts
        public int[] GridLayout => _gridLayout;
        public int[] BackGridLayout => _backGridLayout;
        public int[] DisabledSections => _disabledSections;
        
        [Header("Special Powers")]
        [Tooltip("Enable the random match power for this level")]
        public bool EnableRandomMatchPower = false;
        [Tooltip("Number of times the random match power can be used in this level")]
        public int RandomMatchUses = 1;

        [Tooltip("Enable the shuffle power for this level")]
        public bool EnableShufflePower = false;
        [Tooltip("Number of times the shuffle power can be used in this level")]
        public int ShuffleUses = 1;
        
        [Header("Obstacles")]
        public List<Vector2Int> BlockedCells = new List<Vector2Int>();

        [Tooltip("Whether the level requires clearing all items instead of just meeting sorting goals")]
        public bool ClearAllItemsGoal = true;
        
        #if UNITY_EDITOR
        // Validate that grid dimensions make sense when values are changed in the editor
        private void OnValidate()
        {
            // Ensure grid width is a multiple of number of sections
            if (GridWidth % NumberOfSections != 0)
            {
                // Round to nearest multiple
                GridWidth = Mathf.RoundToInt(GridWidth / (float)NumberOfSections) * NumberOfSections;
                
                // Ensure at least 3 columns per section (for matching)
                int columnsPerSection = GridWidth / NumberOfSections;
                if (columnsPerSection < 3)
                {
                    GridWidth = NumberOfSections * 3;
                }
                
                Debug.LogWarning($"Grid width must be a multiple of NumberOfSections. Adjusted to {GridWidth}");
            }
            
            // Ensure minimum height for matching
            if (GridHeight < 1)
            {
                GridHeight = 1;
                Debug.LogWarning("Grid height must be at least 1. Adjusted to 1.");
            }
            
            // Ensure minimum NumberOfSections
            if (NumberOfSections < 1)
            {
                NumberOfSections = 1;
                Debug.LogWarning("Number of sections must be at least 1. Adjusted to 1.");
            }
            
            // Check if grid layout needs to be recreated
            if (_gridLayout != null && _gridLayout.Length != GridWidth * GridHeight)
            {
                // Grid dimensions changed - grid layout needs to be recreated
                _gridLayout = null;
                _backGridLayout = null;
                Debug.Log("Grid dimensions changed. Grid layout will be recreated.");
            }
        }
        #endif
        
        // Helper method to create default grid layouts if none are specified
        public void EnsureGridLayoutExists()
        {
            // Check front grid layout
            if (_gridLayout == null || _gridLayout.Length != GridWidth * GridHeight)
            {
                Debug.Log($"Creating default front grid layout for level {LevelNumber}");
                
                // Use the DefaultLevelLayout utility to create a matchable layout
                _gridLayout = DefaultLevelLayout.CreateMatchableLayout(GridWidth, GridHeight, NumberOfSections);
            }
            
            // Check back grid layout
            if (_backGridLayout == null || _backGridLayout.Length != GridWidth * GridHeight)
            {
                Debug.Log($"Creating default back grid layout for level {LevelNumber}");
                
                // Create empty back grid layout (all -1)
                _backGridLayout = new int[GridWidth * GridHeight];
                for (int i = 0; i < _backGridLayout.Length; i++)
                {
                    _backGridLayout[i] = -1; // Empty by default
                }
                
                // Randomly add some back items for testing
                // This would be configured manually in the level editor
                for (int i = 0; i < _backGridLayout.Length; i++)
                {
                    if (UnityEngine.Random.value < 0.3f && _gridLayout[i] >= 0)
                    {
                        // 30% chance of having a back item under a front item
                        _backGridLayout[i] = UnityEngine.Random.Range(0, 6); // Random item type 0-5
                    }
                }
            }
        }
        
        // Get front item type at specific grid position (returns -1 for empty cells)
        public int GetItemAt(int x, int y)
        {
            EnsureGridLayoutExists();
            int index = y * GridWidth + x;
            if (index >= 0 && index < _gridLayout.Length)
            {
                return _gridLayout[index];
            }
            return -1;
        }
        
        // Get back item type at specific grid position (returns -1 for empty cells)
        public int GetBackItemAt(int x, int y)
        {
            EnsureGridLayoutExists();
            int index = y * GridWidth + x;
            if (index >= 0 && index < _backGridLayout.Length)
            {
                return _backGridLayout[index];
            }
            return -1;
        }
    }
} 