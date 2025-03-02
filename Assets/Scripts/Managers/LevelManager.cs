using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoodsSorting.Levels;
using GoodsSorting.Grid;

namespace GoodsSorting.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private SortingManager _sortingManager;
        [SerializeField] private List<LevelData> _levelDataList;
        
        [Header("UI References")]
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private GameObject _levelFailedPanel;
        
        private LevelData _currentLevelData;
        private int _currentLevelIndex = 0;
        private float _timeRemaining = 0;
        private bool _isLevelActive = false;
        
        private float _nextLevelCooldown = 0f;
        private const float NEXT_LEVEL_COOLDOWN_TIME = 1.0f; // 1 second cooldown
        
        // Events
        public event Action<int> OnLevelLoaded;
        public event Action<float> OnTimeUpdated;
        public event Action OnLevelCompleted;
        public event Action OnLevelFailed;
        public event Action OnAllLevelsCompleted;
        
        private void Awake()
        {
            // Validate level data list
            ValidateLevelDataList();
        }
        
        private void ValidateLevelDataList()
        {
            if (_levelDataList == null || _levelDataList.Count == 0)
            {
                Debug.LogError("LevelManager: Level data list is null or empty!");
                return;
            }
            
            // Log all levels for debugging
            Debug.Log($"LevelManager: Validating level data list with {_levelDataList.Count} levels");
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] == null)
                {
                    Debug.LogError($"Level {i} data is null! This may cause issues with level progression.");
                }
                else
                {
                    Debug.Log($"Level {i} data: {_levelDataList[i].name}, Level Number: {_levelDataList[i].LevelNumber}");
                    
                    // Check if the level number matches its index
                    if (_levelDataList[i].LevelNumber != i + 1)
                    {
                        Debug.LogWarning($"Level {i}'s LevelNumber property ({_levelDataList[i].LevelNumber}) doesn't match its index in the list (should be {i + 1})");
                    }
                }
            }
            
            // Create a progression map to visualize level progression
            Dictionary<int, int> levelNumberToIndexMap = new Dictionary<int, int>();
            List<int> levelNumbers = new List<int>();
            
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] != null)
                {
                    int levelNumber = _levelDataList[i].LevelNumber;
                    levelNumbers.Add(levelNumber);
                    levelNumberToIndexMap[levelNumber] = i;
                }
            }
            
            // Sort level numbers to see the progression
            levelNumbers.Sort();
            
            // Log the level progression path
            string progressionPath = "Level Progression Path: ";
            for (int i = 0; i < levelNumbers.Count; i++)
            {
                int levelNumber = levelNumbers[i];
                int index = levelNumberToIndexMap[levelNumber];
                progressionPath += $"{levelNumber}(idx:{index})";
                
                if (i < levelNumbers.Count - 1)
                {
                    progressionPath += " -> ";
                }
            }
            
            Debug.Log(progressionPath);
            
            // Check for missing level numbers
            for (int i = 1; i <= levelNumbers.Count; i++)
            {
                if (!levelNumbers.Contains(i))
                {
                    Debug.LogWarning($"Missing level number: {i} in the sequence");
                }
            }
            
            // Fix level ordering issues at runtime
            FixLevelOrder();
        }
        
        private void FixLevelOrder()
        {
            // Check if we need to fix the order
            bool needsFixing = false;
            
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] != null && _levelDataList[i].LevelNumber != i + 1)
                {
                    needsFixing = true;
                    break;
                }
            }
            
            if (!needsFixing)
            {
                // Debug.Log("Level order is correct, no fixing needed.");
                return;
            }
            
            // Debug.LogWarning("Level order needs fixing. Creating a properly ordered list at runtime...");
            
            // Create a new ordered list
            List<LevelData> orderedList = new List<LevelData>(_levelDataList.Count);
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                orderedList.Add(null); // Initialize with nulls
            }
            
            // Place each level at the correct index based on its LevelNumber
            foreach (var levelData in _levelDataList)
            {
                if (levelData != null)
                {
                    int targetIndex = levelData.LevelNumber - 1; // Convert LevelNumber to 0-based index
                    
                    // Make sure target index is valid
                    if (targetIndex >= 0 && targetIndex < orderedList.Count)
                    {
                        if (orderedList[targetIndex] != null)
                        {
                            Debug.LogError($"Multiple levels with same LevelNumber: {levelData.LevelNumber}! " +
                                          $"Conflict between '{orderedList[targetIndex].name}' and '{levelData.name}'");
                        }
                        else
                        {
                            orderedList[targetIndex] = levelData;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Level '{levelData.name}' has invalid LevelNumber: {levelData.LevelNumber} " +
                                      $"(valid range: 1-{orderedList.Count})");
                    }
                }
            }
            
            // Replace the original list with the ordered one
            _levelDataList = orderedList;
            
            // Log the corrected order
            Debug.Log("Level order has been fixed at runtime. New order:");
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] != null)
                {
                    Debug.Log($"Level {i} data: {_levelDataList[i].name}, Level Number: {_levelDataList[i].LevelNumber}");
                }
                else
                {
                    Debug.LogWarning($"Level {i} data is still null after fixing order!");
                }
            }
        }
        
        private void Start()
        {
            // Set all levels as unlocked
            PlayerPrefs.SetInt("LastCompletedLevel", 1000); // Setting a high value to ensure all levels are unlocked
            PlayerPrefs.Save();
            
            // Log level data information
            Debug.Log($"LevelManager Start: Level data list contains {_levelDataList.Count} levels");
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] != null)
                    Debug.Log($"Level {i} data: {_levelDataList[i].name} exists");
                else
                    Debug.Log($"Level {i} data is null!");
            }
            
            // Check if we have valid consecutive level numbers (1, 2, 3, ...)
            bool hasConsecutiveLevels = true;
            List<int> missingLevels = new List<int>();
            HashSet<int> levelNumbers = new HashSet<int>();
            
            // Collect all level numbers
            foreach (var levelData in _levelDataList)
            {
                if (levelData != null)
                {
                    levelNumbers.Add(levelData.LevelNumber);
                }
            }
            
            // Check for missing levels in sequence
            for (int i = 1; i <= levelNumbers.Count; i++)
            {
                if (!levelNumbers.Contains(i))
                {
                    hasConsecutiveLevels = false;
                    missingLevels.Add(i);
                }
            }
            
            if (!hasConsecutiveLevels)
            {
                Debug.LogError($"LEVEL SETUP ERROR: Level sequence is not consecutive. Missing levels: {string.Join(", ", missingLevels)}");
                Debug.LogError("This can cause issues with level progression. Please fix the LevelNumber properties in your level data assets.");
            }
            
            // Subscribe to sorting manager events
            if (_sortingManager != null)
            {
                _sortingManager.OnLevelCompleted += HandleLevelCompleted;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_sortingManager != null)
            {
                _sortingManager.OnLevelCompleted -= HandleLevelCompleted;
            }
        }
        
        private void Update()
        {
            if (_isLevelActive && _currentLevelData.TimeLimit > 0)
            {
                // Update time remaining
                _timeRemaining -= Time.deltaTime;
                
                // Notify listeners
                OnTimeUpdated?.Invoke(_timeRemaining);
                
                // Check for time-based failure
                if (_timeRemaining <= 0)
                {
                    _timeRemaining = 0;
                    FailLevel();
                }
            }
        }
        
        // Added public keyword to make this method accessible to GameManager
        public void LoadLevel(int levelIndex)
        {
            // Validate level index
            if (levelIndex < 0 || levelIndex >= _levelDataList.Count)
            {
                Debug.LogError($"LevelManager: Invalid level index {levelIndex}! Valid range is 0-{_levelDataList.Count - 1}.");
                return;
            }
            
            // Set the current level data
            _currentLevelData = _levelDataList[levelIndex];
            _currentLevelIndex = levelIndex;
            
            if (_currentLevelData == null)
            {
                Debug.LogError($"LevelManager: Level data at index {levelIndex} is null!");
                return;
            }
            
            // Initialize the level
            ResetLevel();
            
            // Set time limit based on level data
            _timeRemaining = _currentLevelData.TimeLimit;
            
            // Start tracking time if this level has a time limit
            if (_timeRemaining > 0)
            {
                StartCoroutine(CountdownTimer());
            }
            
            // Initialize grid and sorting
            if (_gridManager != null)
            {
                _gridManager.ForceReset();
            }
            
            if (_sortingManager != null)
            {
                _sortingManager.InitializeLevel(_currentLevelData);
            }
            
            // Activate level state
            _isLevelActive = true;
            
            // Hide completion panels
            HideCompletionPanels();
            
            // Notify level loaded
            OnLevelLoaded?.Invoke(_currentLevelIndex);
            
            Debug.Log($"LevelManager: Loaded level {_currentLevelData.LevelNumber}");
        }
        
        private void ResetLevel()
        {
            // Stop all running timers
            StopAllCoroutines();
            
            // Reset time
            _timeRemaining = _currentLevelData?.TimeLimit ?? 0;
            
            // Hide completion panels
            HideCompletionPanels();
            
            // Reset board state
            if (_gridManager != null)
            {
                _gridManager.ResetSelectionState();
                _gridManager.StopAllCoroutines();
            }
            
            Debug.Log("LevelManager: Level state reset");
        }
        
        private void HandleLevelCompleted()
        {
            CompleteLevelSuccess();
        }
        
        private void CompleteLevelSuccess()
        {
            if (!_isLevelActive) return;
            
            _isLevelActive = false;
            
            // Show level complete UI
            if (_levelCompletePanel != null)
            {
                _levelCompletePanel.SetActive(true);
            }
            
            // Save progress (in a real implementation this would store to PlayerPrefs or similar)
            int nextLevel = _currentLevelIndex + 1;
            Debug.Log($"CompleteLevelSuccess: Level {_currentLevelIndex} completed, saving as last completed level");
            Debug.Log($"CompleteLevelSuccess: Next level would be {nextLevel}");
            
            PlayerPrefs.SetInt("LastCompletedLevel", _currentLevelIndex);
            PlayerPrefs.Save();
            
            // Notify listeners
            OnLevelCompleted?.Invoke();
        }
        
        private void FailLevel()
        {
            if (!_isLevelActive) return;
            
            _isLevelActive = false;
            
            // Show level failed UI
            if (_levelFailedPanel != null)
            {
                _levelFailedPanel.SetActive(true);
            }
            
            // Notify listeners
            OnLevelFailed?.Invoke();
        }
        
        // Overload with reason parameter
        private void FailLevel(string reason)
        {
            Debug.Log($"Level failed: {reason}");
            FailLevel();
        }
        
        public void RestartLevel()
        {
            Debug.Log("Restarting level with complete state reset");
            
            // Complete reset before restarting
            ResetGameState();
            
            // Now load the level
            LoadLevel(_currentLevelIndex);
        }
        
        // Helper method to reset the entire game state
        public void ResetGameState()
        {
            // Reset level-specific state
            _isLevelActive = false;
            
            // Reset grid
            if (_gridManager != null)
            {
                _gridManager.ResetSelectionState();
                _gridManager.StopAllCoroutines();
                // Force a complete grid reset
                _gridManager.ForceReset();
            }
            
            // Reset sorting
            if (_sortingManager != null)
            {
                _sortingManager.ResetManager();
            }
            
            // Hide any UI panels that might be active
            if (_levelCompletePanel != null)
                _levelCompletePanel.SetActive(false);
                
            if (_levelFailedPanel != null)
                _levelFailedPanel.SetActive(false);
            
            Debug.Log("Game state completely reset");
        }
        
        public void LoadNextLevel()
        {
            // Prevent rapid double-clicks causing multiple level loads
            if (Time.time < _nextLevelCooldown)
            {
                Debug.LogWarning($"LoadNextLevel: Ignoring request - cooldown active. Try again in {_nextLevelCooldown - Time.time:F2} seconds");
                return;
            }
            
            _nextLevelCooldown = Time.time + NEXT_LEVEL_COOLDOWN_TIME;
            
            // Check if we have a current level
            if (_currentLevelData == null)
            {
                Debug.LogError("LoadNextLevel: Cannot load next level - current level data is null");
                return;
            }
            
            int currentLevelNumber = _currentLevelData.LevelNumber;
            Debug.Log($"LoadNextLevel: Current level index: {_currentLevelIndex}, LevelNumber: {currentLevelNumber}");
            
            // Debug all levels to verify what we're working with
            Debug.Log($"LoadNextLevel: There are {_levelDataList.Count} levels in the list");
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] == null)
                {
                    Debug.LogError($"Level at index {i} is null!");
                    continue;
                }
                
                Debug.Log($"Level at index {i}: LevelNumber = {_levelDataList[i].LevelNumber}");
            }
            
            // First try to find the next level by LevelNumber (currentLevelNumber + 1)
            int targetLevelNumber = currentLevelNumber + 1;
            int nextIndex = -1;
            
            Debug.Log($"Looking for level with LevelNumber = {targetLevelNumber}");
            
            for (int i = 0; i < _levelDataList.Count; i++)
            {
                if (_levelDataList[i] != null && _levelDataList[i].LevelNumber == targetLevelNumber)
                {
                    nextIndex = i;
                    Debug.Log($"Found next level at index {i} with LevelNumber {targetLevelNumber}");
                    break;
                }
            }
            
            // If we didn't find the next sequential level, find the level with the next highest LevelNumber
            if (nextIndex == -1)
            {
                Debug.LogWarning($"Could not find level with LevelNumber = {targetLevelNumber}, looking for next highest");
                
                int closestHigherNumber = int.MaxValue;
                
                for (int i = 0; i < _levelDataList.Count; i++)
                {
                    if (_levelDataList[i] != null && 
                        _levelDataList[i].LevelNumber > currentLevelNumber && 
                        _levelDataList[i].LevelNumber < closestHigherNumber)
                    {
                        closestHigherNumber = _levelDataList[i].LevelNumber;
                        nextIndex = i;
                    }
                }
                
                if (nextIndex != -1)
                {
                    Debug.Log($"Found next closest level at index {nextIndex} with LevelNumber {_levelDataList[nextIndex].LevelNumber}");
                }
            }
            
            // If we found a next level, load it
            if (nextIndex != -1)
            {
                Debug.Log($"Loading level at index {nextIndex} with LevelNumber {_levelDataList[nextIndex].LevelNumber}");
                LoadLevel(nextIndex);
                
                // Store last completed level (current level before moving to next)
                PlayerPrefs.SetInt("LastCompletedLevel", currentLevelNumber - 1);
                PlayerPrefs.Save();
            }
            else
            {
                // No next level found, handle completion of all levels
                Debug.Log("No more levels to load - game completed!");
                OnAllLevelsCompleted?.Invoke();
            }
        }
        
        public LevelData GetCurrentLevelData()
        {
            return _currentLevelData;
        }
        
        public int GetCurrentLevelIndex()
        {
            return _currentLevelIndex;
        }
        
        private void HideCompletionPanels()
        {
            if (_levelCompletePanel != null)
                _levelCompletePanel.SetActive(false);
                
            if (_levelFailedPanel != null)
                _levelFailedPanel.SetActive(false);
        }
        
        private IEnumerator CountdownTimer()
        {
            while (_timeRemaining > 0 && _isLevelActive)
            {
                yield return new WaitForSeconds(1f);
                
                if (_isLevelActive)
                {
                    _timeRemaining--;
                    
                    // Notify time update
                    OnTimeUpdated?.Invoke(_timeRemaining);
                    
                    // Check for time-up failure
                    if (_timeRemaining <= 0 && _currentLevelData.TimeLimit > 0)
                    {
                        FailLevel("Time's up!");
                        break;
                    }
                }
            }
        }
        
        // Get the time remaining for UI display
        public float GetTimeRemaining()
        {
            return _timeRemaining;
        }

        // Called by grid when a move is made
        public void RegisterMove()
        {
            if (!_isLevelActive) return;
            
            // Log the move for debugging purposes
            Debug.Log("LevelManager: Move registered");
        }
    }
} 