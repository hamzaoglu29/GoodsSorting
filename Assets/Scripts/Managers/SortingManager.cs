using System;
using System.Collections.Generic;
using UnityEngine;
using GoodsSorting.Grid;
using GoodsSorting.Levels;

namespace GoodsSorting.Managers
{
    public class SortingManager : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        
        private LevelData _currentLevel;
        private Dictionary<int, int> _collectedItems = new Dictionary<int, int>();
        
        // Events
        public event Action<int, int, int> OnItemCollected; // item type, collected count, target count
        public event Action OnLevelCompleted;
        
        private void Start()
        {
            // Subscribe to match events from the grid
            if (_gridManager != null)
            {
                _gridManager.OnItemsMatched += HandleItemsMatched;
            }
            
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            else
            {
                Debug.LogWarning("SortingManager: GameManager instance not found");
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_gridManager != null)
            {
                _gridManager.OnItemsMatched -= HandleItemsMatched;
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        private void OnEnable()
        {
            // Reset when enabled
            ResetManager();
        }
        
        private void OnDisable()
        {
            // Clean up when disabled
            ResetManager();
        }
        
        // Helper method to get the color for an item type (matches GridItem colors)
        private Color GetColorForItemType(int itemType)
        {
            switch (itemType)
            {
                case 0: return Color.red; // Apple
                case 1: return Color.yellow; // Banana
                case 2: return Color.green; // Lime
                case 3: return new Color(1f, 0.5f, 0f); // Orange
                case 4: return new Color(0.5f, 0f, 0.5f); // Grape
                case 5: return new Color(0f, 0.5f, 1f); // Blueberry
                default: return Color.white;
            }
        }
        
        public void InitializeLevel(LevelData levelData)
        {
            _currentLevel = levelData;
            
            // Clear previous collected items
            _collectedItems.Clear();
            
            // Initialize collection count for each item type
            if (levelData.SortingGoals != null)
            {
                foreach (var goal in levelData.SortingGoals)
                {
                    _collectedItems[goal.ItemType] = 0;
                }
            }
            
            Debug.Log($"SortingManager: Initialized level {levelData.LevelNumber} with {levelData.SortingGoals?.Count ?? 0} goals");
        }
        
        // New method to reset the manager state
        public void ResetManager()
        {
            _collectedItems.Clear();
        }
        
        // Check if all goal conditions are met to complete the level
        private void CheckLevelCompletion()
        {
            if (_currentLevel == null) return;
            
            // For ClearAllItems goal, we rely on the GridManager to check if the grid is empty
            if (_currentLevel.ClearAllItemsGoal)
            {
                // Don't trigger completion here - it happens in CheckAllItemsCleared
                return;
            }
            
            // Traditional sorting goals
            bool allGoalsComplete = true;
            
            foreach (var goal in _currentLevel.SortingGoals)
            {
                int collectedCount = 0;
                _collectedItems.TryGetValue(goal.ItemType, out collectedCount);
                
                if (collectedCount < goal.TargetCount)
                {
                    allGoalsComplete = false;
                    break;
                }
            }
            
            if (allGoalsComplete)
            {
                Debug.Log("SortingManager: All sorting goals completed!");
                OnLevelCompleted?.Invoke();
            }
        }
        
        // Called by GridManager when all items are cleared from the grid
        public void CheckAllItemsCleared()
        {
            if (_currentLevel == null) return;
            
            Debug.Log("SortingManager: All items cleared from the grid!");
            OnLevelCompleted?.Invoke();
        }
        
        // Handle grid matches (collect items)
        private void HandleItemsMatched(int itemType, List<GridItem> matchedItems)
        {
            // Add the matched items to our collected count
            if (!_collectedItems.ContainsKey(itemType))
            {
                _collectedItems[itemType] = 0;
            }
            
            int numMatched = matchedItems.Count;
            _collectedItems[itemType] += numMatched;
            
            Debug.Log($"SortingManager: Collected {numMatched} items of type {itemType}. Total: {_collectedItems[itemType]}");
            
            // Get the target count for this item type
            int targetCount = 0;
            if (_currentLevel != null && _currentLevel.SortingGoals != null)
            {
                foreach (var goal in _currentLevel.SortingGoals)
                {
                    if (goal.ItemType == itemType)
                    {
                        targetCount = goal.TargetCount;
                        break;
                    }
                }
            }
            
            // Notify listeners of collection
            OnItemCollected?.Invoke(itemType, _collectedItems[itemType], targetCount);
            
            // Check if this match completes the level
            CheckLevelCompletion();
        }
        
        // Handle game state changes to show/hide containers
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Gameplay)
            {
                // Reset collection counts when entering gameplay
                _collectedItems.Clear();
            }
        }
        
        // Get overall progress (for UI progress bar)
        public float GetOverallProgress()
        {
            if (_currentLevel == null || _currentLevel.SortingGoals == null || _currentLevel.SortingGoals.Count == 0)
                return 0f;
            
            if (_currentLevel.ClearAllItemsGoal && _gridManager != null)
            {
                // For clear all items goal, use percentage of items cleared from grid
                // This is a rough estimate and would need more precise calculation in a real game
                return 0.8f; // Just show high progress since we can't easily calculate
            }
            
            float totalProgress = 0f;
            float totalGoals = 0f;
            
            foreach (var goal in _currentLevel.SortingGoals)
            {
                int collected = 0;
                _collectedItems.TryGetValue(goal.ItemType, out collected);
                
                float goalProgress = goal.TargetCount > 0 ? 
                    Mathf.Clamp01((float)collected / goal.TargetCount) : 1f;
                
                totalProgress += goalProgress;
                totalGoals += 1f;
            }
            
            return totalGoals > 0 ? totalProgress / totalGoals : 0f;
        }
    }
} 