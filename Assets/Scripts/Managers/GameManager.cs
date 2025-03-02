using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoodsSorting.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _levelSelectPanel;
        [SerializeField] private GameObject _gameplayPanel;
        [SerializeField] private GameObject _pausePanel;
        
        // Game state tracking
        private GameState _currentState = GameState.MainMenu;
        private bool _isPaused = false;
        
        // Events
        public event Action<GameState> OnGameStateChanged;
        
        // Enum to track game state
        public enum GameState
        {
            MainMenu,
            LevelSelect,
            Gameplay,
            Paused
        }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Reset time scale in case it was left at 0 from a previous run
            Time.timeScale = 1f;
        }
        
        private void Start()
        {
            // Ensure UI is set up before we set game state
            EnsureUISetup();
            
            // Subscribe to level manager events
            if (_levelManager != null)
            {
                _levelManager.OnLevelCompleted += HandleLevelCompleted;
                _levelManager.OnLevelFailed += HandleLevelFailed;
            }
            
            // Set initial game state
            SetGameState(GameState.MainMenu);
            
            Debug.Log("GameManager: Game initialized, starting at MainMenu state");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_levelManager != null)
            {
                _levelManager.OnLevelCompleted -= HandleLevelCompleted;
                _levelManager.OnLevelFailed -= HandleLevelFailed;
            }
        }
        
        public void StartGame(int levelIndex)
        {
            // Reset pause state and time scale before starting 
            _isPaused = false;
            Time.timeScale = 1f;
            
            // Load the specified level
            if (_levelManager != null)
            {
                _levelManager.LoadLevel(levelIndex);
            }
            
            // Switch to gameplay state
            SetGameState(GameState.Gameplay);
            
            Debug.Log("GameManager: Starting game with fresh state");
        }
        
        public void ReturnToMainMenu()
        {
            // Reset pause state and time scale
            _isPaused = false;
            Time.timeScale = 1f;
            
            // Hide pause panel if it's visible
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);
            }
            
            // Reset game state
            if (_levelManager != null)
            {
                _levelManager.ResetGameState();
            }
            
            // Switch to main menu state
            SetGameState(GameState.MainMenu);
            
            Debug.Log("GameManager: Returned to main menu with reset state");
        }
        
        public void OpenLevelSelect()
        {
            SetGameState(GameState.LevelSelect);
        }
        
        public void TogglePause()
        {
            if (_currentState == GameState.Gameplay)
            {
                if (_isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }
        
        public void PauseGame()
        {
            if (_currentState == GameState.Gameplay && !_isPaused)
            {
                _isPaused = true;
                Time.timeScale = 0f;
                
                // Show pause panel
                if (_pausePanel != null)
                {
                    _pausePanel.SetActive(true);
                }
                
                SetGameState(GameState.Paused);
            }
        }
        
        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                _isPaused = false;
                Time.timeScale = 1f;
                
                // Hide pause panel
                if (_pausePanel != null)
                {
                    _pausePanel.SetActive(false);
                }
                
                SetGameState(GameState.Gameplay);
            }
        }
        
        public void RestartCurrentLevel()
        {
            // Ensure we reset the pause state when restarting
            _isPaused = false;
            Time.timeScale = 1f;
            
            // Hide pause panel if it's visible
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);
            }
            
            if (_levelManager != null)
            {
                _levelManager.RestartLevel();
            }
            
            // Return to gameplay state
            SetGameState(GameState.Gameplay);
            
            Debug.Log("GameManager: Restarted level with pause state cleared.");
        }
        
        public void LoadNextLevel()
        {
            if (_levelManager != null)
            {
                _levelManager.LoadNextLevel();
            }
            
            // Return to gameplay state
            SetGameState(GameState.Gameplay);
        }
        
        private void HandleLevelCompleted()
        {
            // Handle level completion logic if needed
            Debug.Log("Level Completed!");
        }
        
        private void HandleLevelFailed()
        {
            // Handle level failure logic if needed
            Debug.Log("Level Failed!");
        }
        
        private void SetGameState(GameState newState)
        {
            _currentState = newState;
            
            // Update UI panels
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(newState == GameState.MainMenu);
                
            if (_levelSelectPanel != null)
                _levelSelectPanel.SetActive(newState == GameState.LevelSelect);
                
            if (_gameplayPanel != null)
                _gameplayPanel.SetActive(newState == GameState.Gameplay);
            
            // Trigger event
            OnGameStateChanged?.Invoke(newState);
        }
        
        public GameState GetCurrentState()
        {
            return _currentState;
        }
        
        public bool IsPaused()
        {
            return _isPaused;
        }
        
        // Make sure UI panels are properly set up
        private void EnsureUISetup()
        {
            // Main Menu should be active by default
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("GameManager: Main menu panel reference is missing!");
            }
            
            // All other panels should start inactive
            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.SetActive(false);
            }
            
            if (_gameplayPanel != null)
            {
                _gameplayPanel.SetActive(false);
            }
            
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);
            }
        }
    }
} 