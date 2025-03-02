using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoodsSorting.Managers;
using GoodsSorting.Levels;

namespace GoodsSorting.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _levelSelectPanel;
        [SerializeField] private GameObject _gameplayPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private GameObject _levelFailedPanel;
        
        [Header("Gameplay UI")]
        [SerializeField] private TextMeshProUGUI _levelNumberText;
        [SerializeField] private TextMeshProUGUI _timeRemainingText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Button _pauseButton;
        
        [Header("Power UI")]
        [SerializeField] private GameObject _powerUIContainer;
        [SerializeField] private Button _randomMatchPowerButton;
        [SerializeField] private Button _shufflePowerButton;
        [SerializeField] private TextMeshProUGUI _randomMatchUsesText;
        [SerializeField] private TextMeshProUGUI _shuffleUsesText;
        [SerializeField] private Image _randomMatchIcon;
        [SerializeField] private Image _shuffleIcon;
        
        [Header("Level Complete UI")]
        [SerializeField] private TextMeshProUGUI _levelCompleteText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        
        [Header("Level Select UI")]
        [SerializeField] private Transform _levelButtonsContainer;
        [SerializeField] private Button _levelButtonPrefab;
        
        // References to managers
        private GameManager _gameManager;
        private LevelManager _levelManager;
        private SortingManager _sortingManager;
        private PowerManager _powerManager;
        
        private void Start()
        {
            // Get references to managers
            _gameManager = GameManager.Instance;
            _levelManager = FindObjectOfType<LevelManager>();
            _sortingManager = FindObjectOfType<SortingManager>();
            _powerManager = FindObjectOfType<PowerManager>();
            
            // Set up button listeners
            SetupButtonListeners();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Initialize level select UI
            InitializeLevelSelect();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvents();
        }
        
        private void SetupButtonListeners()
        {
            // Main Menu buttons
            if (_mainMenuPanel != null)
            {
                // Find and set up "Play" button
                Button playButton = _mainMenuPanel.GetComponentInChildren<Button>();
                if (playButton != null)
                {
                    playButton.onClick.AddListener(() => _gameManager.OpenLevelSelect());
                }
            }
            
            // Pause button
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(() => _gameManager.PauseGame());
            }
            
            // Power buttons
            if (_randomMatchPowerButton != null && _powerManager != null)
            {
                _randomMatchPowerButton.onClick.AddListener(() => _powerManager.UseRandomMatchPower());
            }
            
            if (_shufflePowerButton != null && _powerManager != null)
            {
                _shufflePowerButton.onClick.AddListener(() => _powerManager.UseShufflePower());
            }
            
            // Pause panel buttons
            if (_pausePanel != null)
            {
                Button resumeButton = _pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                Button restartButton = _pausePanel.transform.Find("RestartButton")?.GetComponent<Button>();
                Button menuButton = _pausePanel.transform.Find("MenuButton")?.GetComponent<Button>();
                
                if (resumeButton != null)
                    resumeButton.onClick.AddListener(() => _gameManager.ResumeGame());
                
                if (restartButton != null)
                    restartButton.onClick.AddListener(() => _gameManager.RestartCurrentLevel());
                
                if (menuButton != null)
                    menuButton.onClick.AddListener(() => _gameManager.ReturnToMainMenu());
            }
            
            // Level Complete panel buttons
            if (_levelCompletePanel != null)
            {
                if (_nextLevelButton != null)
                {
                    // First remove any existing listeners to avoid duplicates
                    _nextLevelButton.onClick.RemoveAllListeners();
                    _nextLevelButton.onClick.AddListener(() => {
                        Debug.Log("UIManager: Next Level button clicked");
                        _gameManager.LoadNextLevel();
                    });
                }
                
                if (_restartButton != null)
                {
                    _restartButton.onClick.RemoveAllListeners();
                    _restartButton.onClick.AddListener(() => _gameManager.RestartCurrentLevel());
                }
                
                if (_mainMenuButton != null)
                {
                    _mainMenuButton.onClick.RemoveAllListeners();
                    _mainMenuButton.onClick.AddListener(() => _gameManager.ReturnToMainMenu());
                }
            }
            
            // Level Failed panel buttons
            if (_levelFailedPanel != null)
            {
                Button retryButton = _levelFailedPanel.transform.Find("RetryButton")?.GetComponent<Button>();
                Button menuButton = _levelFailedPanel.transform.Find("MenuButton")?.GetComponent<Button>();
                
                if (retryButton != null)
                    retryButton.onClick.AddListener(() => _gameManager.RestartCurrentLevel());
                
                if (menuButton != null)
                    menuButton.onClick.AddListener(() => _gameManager.ReturnToMainMenu());
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += HandleGameStateChanged;
            }
            
            if (_levelManager != null)
            {
                _levelManager.OnLevelLoaded += HandleLevelLoaded;
                _levelManager.OnTimeUpdated += HandleTimeUpdated;
                _levelManager.OnLevelCompleted += HandleLevelCompleted;
                _levelManager.OnLevelFailed += HandleLevelFailed;
            }
            
            if (_sortingManager != null)
            {
                _sortingManager.OnItemCollected += HandleItemCollected;
                _sortingManager.OnLevelCompleted += HandleLevelCompleted;
            }
            
            if (_powerManager != null)
            {
                _powerManager.OnRandomMatchPowerUpdated += HandleRandomMatchPowerUpdated;
                _powerManager.OnShufflePowerUpdated += HandleShufflePowerUpdated;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= HandleGameStateChanged;
            }
            
            if (_levelManager != null)
            {
                _levelManager.OnLevelLoaded -= HandleLevelLoaded;
                _levelManager.OnTimeUpdated -= HandleTimeUpdated;
                _levelManager.OnLevelCompleted -= HandleLevelCompleted;
                _levelManager.OnLevelFailed -= HandleLevelFailed;
            }
            
            if (_sortingManager != null)
            {
                _sortingManager.OnItemCollected -= HandleItemCollected;
                _sortingManager.OnLevelCompleted -= HandleLevelCompleted;
            }
            
            if (_powerManager != null)
            {
                _powerManager.OnRandomMatchPowerUpdated -= HandleRandomMatchPowerUpdated;
                _powerManager.OnShufflePowerUpdated -= HandleShufflePowerUpdated;
            }
        }
        
        private void InitializeLevelSelect()
        {
            if (_levelButtonsContainer == null || _levelButtonPrefab == null || _levelManager == null)
                return;
                
            // Clear existing buttons
            foreach (Transform child in _levelButtonsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get available levels
            int levelCount = 5; // For this case study, we only need 5 levels
            int lastCompletedLevel = PlayerPrefs.GetInt("LastCompletedLevel", -1);
            
            // Create level buttons
            for (int i = 0; i < levelCount; i++)
            {
                Button levelButton = Instantiate(_levelButtonPrefab, _levelButtonsContainer);
                TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = $"Level {i + 1}";
                }
                
                // Set all levels as unlocked
                bool isUnlocked = true; // All levels are now unlocked
                levelButton.interactable = isUnlocked;
                
                // Store the level index and set up click handler
                int levelIndex = i;
                levelButton.onClick.AddListener(() => _gameManager.StartGame(levelIndex));
            }
        }
        
        #region Event Handlers
        
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            // Update UI based on game state
            switch (newState)
            {
                case GameManager.GameState.MainMenu:
                    if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
                    if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
                    if (_gameplayPanel != null) _gameplayPanel.SetActive(false);
                    if (_pausePanel != null) _pausePanel.SetActive(false);
                    if (_levelCompletePanel != null) _levelCompletePanel.SetActive(false);
                    if (_levelFailedPanel != null) _levelFailedPanel.SetActive(false);
                    break;
                    
                case GameManager.GameState.LevelSelect:
                    if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
                    if (_levelSelectPanel != null) _levelSelectPanel.SetActive(true);
                    if (_gameplayPanel != null) _gameplayPanel.SetActive(false);
                    if (_pausePanel != null) _pausePanel.SetActive(false);
                    if (_levelCompletePanel != null) _levelCompletePanel.SetActive(false);
                    if (_levelFailedPanel != null) _levelFailedPanel.SetActive(false);
                    break;
                    
                case GameManager.GameState.Gameplay:
                    if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
                    if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
                    if (_gameplayPanel != null) _gameplayPanel.SetActive(true);
                    if (_pausePanel != null) _pausePanel.SetActive(false);
                    if (_levelCompletePanel != null) _levelCompletePanel.SetActive(false);
                    if (_levelFailedPanel != null) _levelFailedPanel.SetActive(false);
                    break;
                    
                case GameManager.GameState.Paused:
                    if (_pausePanel != null) _pausePanel.SetActive(true);
                    break;
            }
        }
        
        private void HandleLevelLoaded(int levelIndex)
        {
            if (_levelNumberText != null)
            {
                _levelNumberText.text = $"Level {levelIndex + 1}";
            }
            
            // Reset the progress bar
            if (_progressSlider != null)
            {
                _progressSlider.value = 0f;
            }
            
            // Get current level data
            LevelData levelData = _levelManager.GetCurrentLevelData();
            if (levelData != null)
            {
                // Update time display
                UpdateTimeDisplay(levelData.TimeLimit);
                
                // Configure power UI based on level settings
                ConfigurePowerUI(levelData);
            }
        }
        
        private void HandleTimeUpdated(float timeRemaining)
        {
            UpdateTimeDisplay(timeRemaining);
        }
        
        private void HandleLevelCompleted()
        {
            if (_gameplayPanel != null)
            {
                _gameplayPanel.SetActive(false);
            }
            
            if (_levelCompletePanel != null)
            {
                _levelCompletePanel.SetActive(true);
            }
            
            if (_levelCompleteText != null)
            {
                int levelIndex = _levelManager.GetCurrentLevelIndex();
                _levelCompleteText.text = $"Level {levelIndex + 1} Completed!";
            }
        }
        
        private void HandleLevelFailed()
        {
            if (_gameplayPanel != null)
            {
                _gameplayPanel.SetActive(false);
            }
            
            if (_levelFailedPanel != null)
            {
                _levelFailedPanel.SetActive(true);
            }
        }
        
        private void HandleItemCollected(int itemType, int collected, int target)
        {
            // Update progress bar based on overall progress
            if (_progressSlider != null && _sortingManager != null)
            {
                _progressSlider.value = _sortingManager.GetOverallProgress();
            }
        }
        
        private void ConfigurePowerUI(LevelData levelData)
        {
            if (_powerUIContainer == null) return;
            
            // Show/hide random match power based on level settings
            if (_randomMatchPowerButton != null && _randomMatchUsesText != null)
            {
                bool enabled = levelData.EnableRandomMatchPower;
                _randomMatchPowerButton.gameObject.SetActive(enabled);
                _randomMatchUsesText.gameObject.SetActive(enabled);
                
                if (enabled)
                {
                    _randomMatchUsesText.text = levelData.RandomMatchUses.ToString();
                    _randomMatchPowerButton.interactable = (levelData.RandomMatchUses > 0);
                }
            }
            
            // Show/hide shuffle power based on level settings
            if (_shufflePowerButton != null && _shuffleUsesText != null)
            {
                bool enabled = levelData.EnableShufflePower;
                _shufflePowerButton.gameObject.SetActive(enabled);
                _shuffleUsesText.gameObject.SetActive(enabled);
                
                if (enabled)
                {
                    _shuffleUsesText.text = levelData.ShuffleUses.ToString();
                    _shufflePowerButton.interactable = (levelData.ShuffleUses > 0);
                }
            }
            
            // Hide power container if no powers are enabled
            _powerUIContainer.SetActive(levelData.EnableRandomMatchPower || levelData.EnableShufflePower);
        }
        
        private void HandleRandomMatchPowerUpdated(int usesRemaining)
        {
            if (_randomMatchUsesText != null)
            {
                _randomMatchUsesText.text = usesRemaining.ToString();
            }
            
            if (_randomMatchPowerButton != null)
            {
                _randomMatchPowerButton.interactable = (usesRemaining > 0);
            }
        }
        
        private void HandleShufflePowerUpdated(int usesRemaining)
        {
            if (_shuffleUsesText != null)
            {
                _shuffleUsesText.text = usesRemaining.ToString();
            }
            
            if (_shufflePowerButton != null)
            {
                _shufflePowerButton.interactable = (usesRemaining > 0);
            }
        }
        
        #endregion
        
        #region UI Helper Methods
        
        private void UpdateTimeDisplay(float timeRemaining)
        {
            if (_timeRemainingText != null)
            {
                if (timeRemaining > 0)
                {
                    int minutes = Mathf.FloorToInt(timeRemaining / 60);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60);
                    
                    string timeText = string.Format("{0:00}:{1:00}", minutes, seconds);
                    _timeRemainingText.text = timeText;
                    _timeRemainingText.gameObject.SetActive(true);
                }
                else if (timeRemaining == 0)
                {
                    // Unlimited time
                    _timeRemainingText.gameObject.SetActive(false);
                }
                else
                {
                    // Negative time = time's up
                    _timeRemainingText.text = "Time's Up!";
                    _timeRemainingText.gameObject.SetActive(true);
                }
            }
        }
        
        #endregion
    }
} 