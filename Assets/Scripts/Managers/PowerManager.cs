using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoodsSorting.Grid;
using GoodsSorting.Levels;
using Random = UnityEngine.Random;

namespace GoodsSorting.Managers
{
    public class PowerManager : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelManager _levelManager;

        // Power state
        private int _randomMatchUsesRemaining = 0;
        private int _shuffleUsesRemaining = 0;
        private bool _powerActive = false;

        // Events
        public event Action<int> OnRandomMatchPowerUpdated;
        public event Action<int> OnShufflePowerUpdated;

        private void Start()
        {
            // Subscribe to level loaded event
            if (_levelManager != null)
            {
                _levelManager.OnLevelLoaded += HandleLevelLoaded;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_levelManager != null)
            {
                _levelManager.OnLevelLoaded -= HandleLevelLoaded;
            }
        }

        private void HandleLevelLoaded(int levelIndex)
        {
            // Get the current level data
            LevelData levelData = _levelManager.GetCurrentLevelData();
            if (levelData == null) return;

            // Reset power uses
            _randomMatchUsesRemaining = levelData.EnableRandomMatchPower ? levelData.RandomMatchUses : 0;
            _shuffleUsesRemaining = levelData.EnableShufflePower ? levelData.ShuffleUses : 0;

            // Notify UI of updated power uses
            OnRandomMatchPowerUpdated?.Invoke(_randomMatchUsesRemaining);
            OnShufflePowerUpdated?.Invoke(_shuffleUsesRemaining);
        }

        public bool CanUseRandomMatchPower()
        {
            return _randomMatchUsesRemaining > 0 && !_powerActive;
        }

        public bool CanUseShufflePower()
        {
            return _shuffleUsesRemaining > 0 && !_powerActive;
        }

        public void UseRandomMatchPower()
        {
            if (!CanUseRandomMatchPower()) return;

            _powerActive = true;
            StartCoroutine(ExecuteRandomMatchPower());
        }

        public void UseShufflePower()
        {
            if (!CanUseShufflePower()) return;

            _powerActive = true;
            StartCoroutine(ExecuteShufflePower());
        }

        private IEnumerator ExecuteRandomMatchPower()
        {
            Debug.Log("Using Random Match Power");

            // Decrement uses
            _randomMatchUsesRemaining--;
            OnRandomMatchPowerUpdated?.Invoke(_randomMatchUsesRemaining);

            // Find all items on the grid
            List<GridItem> allItems = new List<GridItem>();
            for (int x = 0; x < _gridManager.GridWidth; x++)
            {
                for (int y = 0; y < _gridManager.GridHeight; y++)
                {
                    GridItem item = _gridManager.GetItemAt(x, y);
                    if (item != null)
                    {
                        allItems.Add(item);
                    }
                }
            }

            // If we have fewer than 3 items, abort
            if (allItems.Count < 3)
            {
                Debug.LogWarning("Not enough items to use Random Match Power");
                _powerActive = false;
                yield break;
            }

            // Group items by type
            Dictionary<int, List<GridItem>> itemsByType = new Dictionary<int, List<GridItem>>();
            foreach (GridItem item in allItems)
            {
                if (!itemsByType.ContainsKey(item.ItemType))
                {
                    itemsByType[item.ItemType] = new List<GridItem>();
                }
                itemsByType[item.ItemType].Add(item);
            }

            // Find a type with at least 3 items
            List<int> validTypes = new List<int>();
            foreach (var pair in itemsByType)
            {
                if (pair.Value.Count >= 3)
                {
                    validTypes.Add(pair.Key);
                }
            }

            if (validTypes.Count == 0)
            {
                Debug.LogWarning("No item type has enough items for Random Match Power");
                _powerActive = false;
                yield break;
            }

            // Select a random valid type
            int selectedType = validTypes[Random.Range(0, validTypes.Count)];
            List<GridItem> matchableItems = itemsByType[selectedType];

            // Randomly select 3 items of this type
            List<GridItem> itemsToMatch = new List<GridItem>();
            for (int i = 0; i < 3; i++)
            {
                int randomIndex = Random.Range(0, matchableItems.Count);
                itemsToMatch.Add(matchableItems[randomIndex]);
                matchableItems.RemoveAt(randomIndex);
            }

            // Highlight the selected items briefly
            foreach (GridItem item in itemsToMatch)
            {
                item.SetHighlighted(true);
            }

            // Wait a moment so player can see the selected items
            yield return new WaitForSeconds(0.5f);

            // Remove highlight
            foreach (GridItem item in itemsToMatch)
            {
                item.SetHighlighted(false);
            }

            // Process the match
            yield return _gridManager.ProcessCustomMatch(itemsToMatch);
            
            // Check if this was the last items on the grid
            yield return new WaitForSeconds(0.5f);
            if (_gridManager.IsGridEmpty())
            {
                Debug.Log("PowerManager: Grid is now empty after using Random Match power!");
                
                // Find the SortingManager to check level completion
                SortingManager sortingManager = FindObjectOfType<SortingManager>();
                if (sortingManager != null)
                {
                    Debug.Log("PowerManager: Notifying SortingManager that all items are cleared.");
                    sortingManager.CheckAllItemsCleared();
                }
            }

            _powerActive = false;
        }

        private IEnumerator ExecuteShufflePower()
        {
            Debug.Log("Using Shuffle Power");

            // Decrement uses
            _shuffleUsesRemaining--;
            OnShufflePowerUpdated?.Invoke(_shuffleUsesRemaining);

            // Wait for animation
            yield return new WaitForSeconds(0.2f);

            // Perform the shuffle
            yield return _gridManager.ShuffleGrid();

            _powerActive = false;
        }
    }
} 