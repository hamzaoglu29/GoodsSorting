using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoodsSorting.Managers;
using GoodsSorting.Levels;

namespace GoodsSorting.Grid
{
    public class GridManager : MonoBehaviour
    {
        // Updated grid system with:
        // - 2-5 empty spaces on the grid
        // - No new items spawning or falling
        // - Only row matches (no column matches)
        // - New swap mechanic: select an item then select an empty space
        // - Multi-layer support: items can be in front or back layer
        
        [Header("Grid Settings")]
        [SerializeField] private int _gridWidth = 9;
        [SerializeField] private int _gridHeight = 4;
        [SerializeField] private float _cellSize = 1.0f;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _sectionSeparatorPrefab;
        
        // Added: Number of sections horizontally
        [SerializeField] private int _numberOfSections = 3;
        [SerializeField] private Color _sectionSeparatorColor = new Color(0.8f, 0.6f, 0.4f, 0.8f);
        
        // Properties for external access
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public int NumberOfSections => _numberOfSections;
        public int ColumnsPerSection => _gridWidth / _numberOfSections;
        
        [Header("Item Settings")]
        [SerializeField] private GameObject[] _itemPrefabs;
        [SerializeField] private float _swapSpeed = 0.3f;
        
        [Header("References")]
        [SerializeField] private LevelManager _levelManager;
        
        // Grid data structure to hold references to all item slots
        private GridItem[,] _grid;
        
        // New grid for back layer items
        private GridItem[,] _backGrid;
        
        // Selected items for swapping
        private GridItem _selectedItem;
        private Vector2Int _selectedPosition;
        
        // Drag and drop variables
        private bool _isDragging = false;
        private Vector3 _dragOffset;
        private Vector3 _originalPosition;
        
        // Events
        public event Action<int, List<GridItem>> OnItemsMatched;
        public event Action OnGridFilled;
        public event Action OnBoardStable;
        public event Action<int, int> OnItemSelected;  // Added for visualization
        public event Action OnSelectionCleared;  // Added for visualization
        
        private bool _isSwapping = false;
        private bool _isProcessingMatches = false;
        private bool _isFillingBoard = false;
        
        // Add an array to track disabled sections
        private HashSet<int> _disabledSections = new HashSet<int>();
        
        // Add a method to check for screen size changes
        private Vector2 _lastScreenSize = Vector2.zero;
        
        private void Awake()
        {
            // Initialize the grid arrays with the correct dimensions
            // Will be properly resized once LevelData is loaded
            _grid = new GridItem[_gridWidth, _gridHeight];
            _backGrid = new GridItem[_gridWidth, _gridHeight];
            
            // Ensure grid is hidden at startup
            if (_gridContainer != null)
            {
                _gridContainer.gameObject.SetActive(false);
            }
            
            // We'll validate that grid width is divisible by number of sections in InitializeGrid
            // after loading from LevelData
            
            // Initialize disabled sections set
            _disabledSections = new HashSet<int>();
            
            // Log grid dimensions for debugging
            Debug.Log($"Grid initialized with default dimensions: {_gridWidth}x{_gridHeight}");
        }
        
        private void Start()
        {
            if (_gridContainer == null)
            {
                _gridContainer = transform.Find("GridContainer");
                if (_gridContainer == null)
                {
                    _gridContainer = new GameObject("GridContainer").transform;
                    _gridContainer.SetParent(transform);
                    // Don't set position here - we'll do it properly in CenterGridContainer
                }
            }
            
            // Initialize screen size tracking
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            
            // No longer hardcoding grid dimensions
            // Will be set from LevelData during InitializeGrid
            
            // Find level manager if not assigned
            if (_levelManager == null)
            {
                _levelManager = FindObjectOfType<LevelManager>();
                if (_levelManager == null)
                {
                    Debug.LogError("GridManager: Could not find LevelManager in scene! Moves will not be tracked.");
                }
                else
                {
                    Debug.Log("GridManager: Successfully found LevelManager reference.");
                }
            }
            
            // Hide the grid container at startup
            _gridContainer.gameObject.SetActive(false);
            
            // Initialize the grid array but don't create items yet
            _grid = new GridItem[_gridWidth, _gridHeight];
            
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            else
            {
                Debug.LogWarning("GridManager: GameManager instance not found");
            }
            
            // No longer initializing grid here - will be done when game state changes to Gameplay
            Debug.Log("GridManager Start: Grid container hidden at startup");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        // Handle game state changes to show/hide grid
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Gameplay)
            {
                // Show grid and initialize if needed
                _gridContainer.gameObject.SetActive(true);
                
                if (IsGridEmpty())
                {
                    InitializeGrid();
                    Debug.Log("GridManager: Initializing grid for gameplay state");
                }
            }
            else
            {
                // Hide grid in menu states
                _gridContainer.gameObject.SetActive(false);
                Debug.Log("GridManager: Hiding grid for menu state");
            }
        }
        
        // Helper to check if grid is empty
        public bool IsGridEmpty()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y] != null || _backGrid[x, y] != null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public void InitializeGrid()
        {
            // Clear existing grid if any
            ClearGrid();
            
            // Get the current level data to use its grid layout
            LevelData currentLevelData = null;
            if (_levelManager != null)
            {
                currentLevelData = _levelManager.GetCurrentLevelData();
            }
            
            if (currentLevelData == null)
            {
                Debug.LogWarning("GridManager: No level data available, using default grid layout");
            }
            else
            {
                // Apply dimensions from the level data
                _gridWidth = currentLevelData.GridWidth;
                _gridHeight = currentLevelData.GridHeight;
                _numberOfSections = currentLevelData.NumberOfSections;
                
                // Log the grid dimensions we're using
                Debug.Log($"Using level data dimensions: {_gridWidth}x{_gridHeight} with {_numberOfSections} sections");
                
                // Ensure the grid layout exists in the level data
                currentLevelData.EnsureGridLayoutExists();
                
                // Load disabled sections from level data
                _disabledSections.Clear();
                if (currentLevelData.DisabledSections != null && currentLevelData.DisabledSections.Length > 0)
                {
                    foreach (int sectionIndex in currentLevelData.DisabledSections)
                    {
                        if (sectionIndex >= 0 && sectionIndex < _numberOfSections)
                        {
                            _disabledSections.Add(sectionIndex);
                            Debug.Log($"Section {sectionIndex} is disabled for this level");
                        }
                    }
                }
                
                Debug.Log($"Using level data grid layout for level {currentLevelData.LevelNumber}");
            }
            
            // Reinitialize the grid arrays with the new dimensions
            _grid = new GridItem[_gridWidth, _gridHeight];
            _backGrid = new GridItem[_gridWidth, _gridHeight];
            
            Debug.Log($"Creating grid with dimensions: {_gridWidth}x{_gridHeight} with {_numberOfSections} sections");
            
            // Center the grid container based on the current screen size and grid dimensions
            CenterGridContainer();
            
            // Create new grid cells based on the level data
            for (int x = 0; x < _gridWidth; x++)
            {
                // Skip creating items in disabled sections
                int section = x / (_gridWidth / _numberOfSections);
                if (_disabledSections.Contains(section))
                {
                    continue;
                }
                
                for (int y = 0; y < _gridHeight; y++)
                {
                    int itemType = -1;
                    int backItemType = -1;
                    
                    if (currentLevelData != null)
                    {
                        // Get item type from the level data
                        itemType = currentLevelData.GetItemAt(x, y);
                        
                        // Check if there's a back layer item defined for this position
                        backItemType = currentLevelData.GetBackItemAt(x, y);
                    }
                    else
                    {
                        // Fallback to a reasonable default pattern if no level data
                        // Make sure every 3 consecutive cells in a row have the same item
                        int sectionIndex = x / 3;
                        itemType = (sectionIndex + y) % _itemPrefabs.Length;
                        
                        // Create some empty spaces (about 15% of cells)
                        if ((x == 1 && y == 1) || (x == 4 && y == 2) || (x == 7 && y == 0))
                        {
                            itemType = -1; // Empty cell
                        }
                        
                        // For testing: Add back layer items in some cells (about 30% of cells)
                        if (UnityEngine.Random.value < 0.3f)
                        {
                            backItemType = UnityEngine.Random.Range(0, _itemPrefabs.Length);
                        }
                    }
                    
                    // Create front layer item if needed
                    if (itemType >= 0 && itemType < _itemPrefabs.Length)
                    {
                        // Create the front item at this position
                        CreateItemOfTypeAt(x, y, itemType, false);
                    }
                    else
                    {
                        // Empty space in front layer
                        _grid[x, y] = null;
                    }
                    
                    // Create back layer item if needed
                    if (backItemType >= 0 && backItemType < _itemPrefabs.Length)
                    {
                        // Create the back item at this position
                        CreateItemOfTypeAt(x, y, backItemType, true);
                    }
                    else
                    {
                        // Empty space in back layer
                        _backGrid[x, y] = null;
                    }
                }
            }
            
            // Create section separators
            CreateSectionSeparators();
            
            // Refresh back layer positions to ensure proper visibility
            RefreshBackLayerPositions();
            
            // Check for empty sections and promote back layer items immediately after initialization
            StartCoroutine(InitialCheckForEmptySections());
            
            // Notify listeners that the grid is filled and stable
            OnGridFilled?.Invoke();
            OnBoardStable?.Invoke();
            
            Debug.Log("Grid initialized with predefined layout");
        }
        
        // Add this new method to center the grid container
        private void CenterGridContainer()
        {
            if (_gridContainer == null) 
            {
                Debug.LogError("Cannot center grid container - grid container is null!");
                return;
            }
            
            // Calculate the total width and height of the grid in world units
            float gridWidth = _gridWidth * _cellSize;
            float gridHeight = _gridHeight * _cellSize;
            
            // Get screen bounds in world coordinates
            Vector3 screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
            
            float screenWidth = screenTopRight.x - screenBottomLeft.x;
            float screenHeight = screenTopRight.y - screenBottomLeft.y;
            
            // Calculate center position
            float centerX = screenBottomLeft.x + screenWidth / 2 - gridWidth / 2;
            float centerY = screenBottomLeft.y + screenHeight / 2 - gridHeight / 2;
            
            // Adjust Y position to be slightly above center
            centerY += screenHeight * 0.1f;
            
            // Use direct world position instead of dealing with parent-relative transforms
            _gridContainer.position = new Vector3(centerX, centerY, 0);
            
            Debug.Log($"[GRID CENTERING] Screen bounds: {screenBottomLeft} to {screenTopRight}");
            Debug.Log($"[GRID CENTERING] Grid size: {gridWidth}x{gridHeight}, Screen size: {screenWidth}x{screenHeight}");
            Debug.Log($"[GRID CENTERING] Positioned grid at: {_gridContainer.position}");
        }
        
        // New coroutine to perform initial check with a small delay
        private IEnumerator InitialCheckForEmptySections()
        {
            // Wait for one frame to ensure all items are properly initialized
            yield return null;
            
            // Log the grid state before checking
            LogGridState();
            
            // Perform the check
            CheckAndPromoteEmptySections();
            
            // Log the grid state after checking
            LogGridState();
        }
        
        // Helper method to log the grid state for debugging
        private void LogGridState()
        {
            string gridStateLog = "Current Grid State:\n";
            
            for (int y = _gridHeight - 1; y >= 0; y--)
            {
                string rowFront = "Front: ";
                string rowBack = "Back:  ";
                
                for (int x = 0; x < _gridWidth; x++)
                {
                    // Log front layer
                    if (_grid[x, y] != null)
                    {
                        rowFront += _grid[x, y].ItemType.ToString() + " ";
                    }
                    else
                    {
                        rowFront += "- ";
                    }
                    
                    // Log back layer
                    if (_backGrid[x, y] != null)
                    {
                        rowBack += _backGrid[x, y].ItemType.ToString() + " ";
                    }
                    else
                    {
                        rowBack += "- ";
                    }
                }
                
                gridStateLog += rowFront + "\n" + rowBack + "\n";
            }
            
            // Comment out in production to reduce log spam
            // Debug.Log(gridStateLog);
        }
        
        // Modified method to create an item of a specific type in either front or back layer
        private void CreateItemOfTypeAt(int x, int y, int itemType, bool isBackLayer)
        {
            // Safety check
            if (itemType < 0 || itemType >= _itemPrefabs.Length)
            {
                Debug.LogError($"Invalid item type {itemType}");
                return;
            }
            
            // Get the appropriate grid reference
            GridItem[,] targetGrid = isBackLayer ? _backGrid : _grid;
            
            // Safety check - destroy any existing item at this position first
            if (targetGrid[x, y] != null)
            {
                Debug.LogWarning($"Destroying existing item at ({x},{y}) in {(isBackLayer ? "back" : "front")} layer before creating a new one");
                Destroy(targetGrid[x, y].gameObject);
                targetGrid[x, y] = null;
            }
            
            // Calculate position based on grid coordinates with perfect centering
            Vector3 position = new Vector3(
                (x + 0.5f) * _cellSize, 
                (y + 0.5f) * _cellSize, 
                isBackLayer ? 0.2f : -0.1f  // Back layer items slightly behind front items
            );
            
            // Instantiate the item of the specified type
            GameObject itemObj = Instantiate(_itemPrefabs[itemType], position, Quaternion.identity, _gridContainer);
            itemObj.name = $"Item_{x}_{y}_{itemType}_{(isBackLayer ? "Back" : "Front")}";
            
            // Get or add GridItem component
            GridItem gridItem = itemObj.GetComponent<GridItem>();
            if (gridItem == null)
            {
                gridItem = itemObj.AddComponent<GridItem>();
            }
            
            // Ensure the sprite renderer has the right sorting order
            SpriteRenderer renderer = itemObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = isBackLayer ? 3 : 5; // Back items lower than front items but above background
            }
            
            // Set up item properties
            gridItem.SetGridPosition(x, y);
            gridItem.SetItemType(itemType);
            gridItem.SetBackLayer(isBackLayer);
            
            // Check if we need to position with offset (only for back layer items)
            if (isBackLayer)
            {
                // Check if there's a front item at this position
                bool hasItemInFront = _grid[x, y] != null;
                gridItem.PositionWithOffset(position, hasItemInFront);
            }
            
            // Store in the appropriate grid array
            targetGrid[x, y] = gridItem;
        }
        
        private void ClearGrid()
        {
            // Clear both front and back grids
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    // Clear front grid
                    if (_grid != null && _grid[x, y] != null)
                    {
                        Destroy(_grid[x, y].gameObject);
                        _grid[x, y] = null;
                    }
                    
                    // Clear back grid
                    if (_backGrid != null && _backGrid[x, y] != null)
                    {
                        Destroy(_backGrid[x, y].gameObject);
                        _backGrid[x, y] = null;
                    }
                }
            }
        }
        
        // Update method to handle drag and drop input
        private void Update()
        {
            if (_isSwapping || _isProcessingMatches || _isFillingBoard)
                return;
                
            if (Input.GetMouseButtonDown(0))
            {
                // Start dragging
                HandleMouseDown();
            }
            else if (_isDragging && Input.GetMouseButton(0))
            {
                // Continue dragging
                HandleMouseDrag();
            }
            else if (_isDragging && Input.GetMouseButtonUp(0))
            {
                // Drop the item
                HandleMouseUp();
            }
        }
        
        // Add a method to check for screen size changes
        private void LateUpdate()
        {
            // Check if the screen size has changed
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
            if (currentScreenSize != _lastScreenSize && _gridContainer != null && _gridContainer.gameObject.activeInHierarchy)
            {
                // Screen size changed, recenter grid
                CenterGridContainer();
                _lastScreenSize = currentScreenSize;
                Debug.Log("Screen size changed, recentered grid");
            }
        }
        
        private void HandleMouseDown()
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Convert to grid coordinates
            int x = Mathf.FloorToInt(mouseWorldPos.x / _cellSize);
            int y = Mathf.FloorToInt(mouseWorldPos.y / _cellSize);
            
            // Check if valid grid position
            if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            {
                // Check if there's an item at this position
                if (_grid[x, y] != null)
                {
                    _selectedItem = _grid[x, y];
                    _selectedPosition = new Vector2Int(x, y);
                    _isDragging = true;
                    
                    // Calculate drag offset (so the item doesn't jump to mouse position)
                    _dragOffset = _selectedItem.transform.position - new Vector3(mouseWorldPos.x, mouseWorldPos.y, _selectedItem.transform.position.z);
                    
                    // Store original position for returning if drag is canceled
                    _originalPosition = _selectedItem.transform.position;
                    
                    // Update visuals
                    _selectedItem.SetSelected(true);
                    
                    // Move item to front during dragging
                    SpriteRenderer renderer = _selectedItem.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 15; // Higher than separators (10) during dragging
                    }
                    
                    // Notify for visualization
                    OnItemSelected?.Invoke(x, y);
                    
                    Debug.Log($"Started dragging item at ({x}, {y})");
                }
            }
        }
        
        private void HandleMouseDrag()
        {
            if (_selectedItem == null) return;
            
            // Update item position with mouse
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _selectedItem.transform.position = new Vector3(
                mouseWorldPos.x + _dragOffset.x, 
                mouseWorldPos.y + _dragOffset.y, 
                -0.1f // Keep z-position consistent to stay in front of separators
            );
        }
        
        private void HandleMouseUp()
        {
            if (_selectedItem == null) return;
            
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Convert to grid coordinates
            int targetX = Mathf.FloorToInt(mouseWorldPos.x / _cellSize);
            int targetY = Mathf.FloorToInt(mouseWorldPos.y / _cellSize);
            
            // Reset dragging state
            _isDragging = false;
            
            // Check if valid drop position
            if (targetX >= 0 && targetX < _gridWidth && targetY >= 0 && targetY < _gridHeight)
            {
                // Check if the target position is empty
                if (_grid[targetX, targetY] == null)
                {
                    // Valid drop on empty space - start swapping animation
                    StartCoroutine(SwapWithEmpty(_selectedPosition.x, _selectedPosition.y, targetX, targetY));
                    Debug.Log($"Moving item from ({_selectedPosition.x}, {_selectedPosition.y}) to ({targetX}, {targetY})");
                }
                else if (targetX == _selectedPosition.x && targetY == _selectedPosition.y)
                {
                    // Dropped on original position - just reset
                    ResetDraggedItem();
                    Debug.Log("Item returned to original position");
                }
                else
                {
                    // Dropped on another item - invalid move
                    ResetDraggedItem();
                    Debug.Log("Invalid drop location (not empty)");
                }
            }
            else
            {
                // Dropped outside grid - invalid move
                ResetDraggedItem();
                Debug.Log("Dropped outside valid grid area");
            }
            
            // Clear selection state
            _selectedItem.SetSelected(false);
            _selectedItem = null;
            OnSelectionCleared?.Invoke();
        }
        
        private void ResetDraggedItem()
        {
            if (_selectedItem != null)
            {
                // Return item to original position
                _selectedItem.transform.position = _originalPosition;
                
                // Reset sorting order back to normal
                SpriteRenderer renderer = _selectedItem.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 5; // Reset to normal sorting order
                }
            }
        }
        
        // Adding back the SwapWithEmpty method
        private IEnumerator SwapWithEmpty(int itemX, int itemY, int emptyX, int emptyY)
        {
            _isSwapping = true;
            
            GridItem item = _grid[itemX, itemY];
            
            // Register a move
            if (_levelManager != null)
            {
                _levelManager.RegisterMove();
                Debug.Log("Registered move for swap attempt!");
            }
            else
            {
                Debug.LogError("GridManager: Cannot register move - _levelManager is null! Check inspector reference.");
            }
            
            // Animate the move
            float elapsedTime = 0f;
            // Use the item's current position instead of calculating from grid coordinates
            Vector3 startPos = item.transform.position;
            Vector3 endPos = new Vector3(
                (emptyX + 0.5f) * _cellSize, 
                (emptyY + 0.5f) * _cellSize, 
                -0.1f  // Keep z-position consistent to stay in front of separators
            );
            
            // Ensure the item's sprite renderer has higher sorting order during animation
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 15; // Higher than separators (10) during movement
            }
            
            while (elapsedTime < _swapSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / _swapSpeed);
                
                item.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                yield return null;
            }
            
            // Ensure final position is exact
            item.transform.position = endPos;
            
            // Reset sorting order back to normal after animation
            if (renderer != null)
            {
                renderer.sortingOrder = 5;
            }
            
            // Update grid references
            _grid[itemX, itemY] = null; // Original position is now empty
            _grid[emptyX, emptyY] = item; // New position now has the item
            
            // Update item grid position
            item.SetGridPosition(emptyX, emptyY);
            
            // Refresh back layer positions to update visuals
            RefreshBackLayerPositions();
            
            // Check for empty sections after a swap
            CheckAndPromoteEmptySections();
            
            // Check for matches
            yield return StartCoroutine(CheckForMatchesCoroutine());
            
            OnBoardStable?.Invoke();
            _isSwapping = false;
        }
        
        private bool HasMatchAt(int x, int y)
        {
            if (_grid[x, y] == null) return false;
            
            int itemType = _grid[x, y].ItemType;
            
            // Check for horizontal match
            if (x > 1 && 
                _grid[x-1, y] != null && _grid[x-2, y] != null &&
                _grid[x-1, y].ItemType == itemType && 
                _grid[x-2, y].ItemType == itemType)
            {
                return true;
            }
            
            // We no longer check for vertical matches
            
            return false;
        }
        
        private bool CheckForMatches()
        {
            bool hasMatches = false;
            
            // Check for matches in horizontal direction only (rows)
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y] != null)
                    {
                        // Check for horizontal matches
                        if (x < _gridWidth - 2)
                        {
                            if (_grid[x + 1, y] != null && _grid[x + 2, y] != null &&
                                _grid[x, y].ItemType == _grid[x + 1, y].ItemType &&
                                _grid[x, y].ItemType == _grid[x + 2, y].ItemType)
                            {
                                hasMatches = true;
                            }
                        }
                        
                        // We no longer check for vertical matches
                    }
                }
            }
            
            return hasMatches;
        }
        
        // New coroutine method that uses CheckForMatches
        private IEnumerator CheckForMatchesCoroutine()
        {
            bool hasMatches = CheckForMatches();
            if (hasMatches)
            {
                yield return StartCoroutine(ProcessMatches());
            }
        }
        
        private IEnumerator ProcessMatches()
        {
            _isProcessingMatches = true;
            
            // Temporary storage for matches
            bool[,] isMatched = new bool[_gridWidth, _gridHeight];
            Dictionary<int, List<GridItem>> matchedItems = new Dictionary<int, List<GridItem>>();
            
            // Find all matches
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y] != null && !isMatched[x, y])
                    {
                        // Horizontal match check (minimum 3)
                        List<GridItem> horizontalMatches = FindHorizontalMatchAt(x, y);
                        if (horizontalMatches.Count >= 3)
                        {
                            foreach (GridItem item in horizontalMatches)
                            {
                                isMatched[item.GridX, item.GridY] = true;
                                
                                // Add to matched items by type
                                if (!matchedItems.ContainsKey(item.ItemType))
                                {
                                    matchedItems[item.ItemType] = new List<GridItem>();
                                }
                                matchedItems[item.ItemType].Add(item);
                            }
                        }
                        
                        // No longer checking for vertical matches
                    }
                }
            }
            
            // Create a list to track positions where we need to promote back items
            List<Vector2Int> positionsToPromote = new List<Vector2Int>();
            
            // Process the matches
            foreach (var kvp in matchedItems)
            {
                int itemType = kvp.Key;
                List<GridItem> items = kvp.Value;
                
                // Trigger the match event with item type and matched items
                OnItemsMatched?.Invoke(itemType, items);
                
                // Destroy the matched items and clear grid references
                foreach (GridItem item in items)
                {
                    int x = item.GridX;
                    int y = item.GridY;
                    
                    // Save this position to check for back items to promote later
                    positionsToPromote.Add(new Vector2Int(x, y));
                    
                    // Only destroy if not already destroyed
                    if (_grid[x, y] != null)
                    {
                        // Ensure item stays in front of separators during animation
                        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
                        if (renderer != null)
                        {
                            renderer.sortingOrder = 15; // Higher than separators during animation
                        }
                        
                        // Play match animation/effect
                        item.PlayMatchEffect();
                        
                        // Wait for animation to complete
                        yield return new WaitForSeconds(0.1f);
                        
                        Destroy(item.gameObject);
                        _grid[x, y] = null;
                    }
                }
            }
            
            // Wait for a short time before promoting back layer items
            yield return new WaitForSeconds(0.3f);
            
            // No longer promote individual back items where matches occurred
            // Instead, only rely on the section-based promotion
            // foreach (Vector2Int pos in positionsToPromote)
            // {
            //    PromoteBackItemToFront(pos.x, pos.y);
            // }
            
            // Check for new matches after promotion
            if (positionsToPromote.Count > 0)
            {
                // yield return new WaitForSeconds(0.3f); // No need to wait for individual promotions
                
                // Check for empty sections and promote all back items in those sections
                CheckAndPromoteEmptySections();
                
                yield return new WaitForSeconds(0.3f); // Give time for section promotion animations
                
                yield return StartCoroutine(CheckForMatchesCoroutine());
            }
            
            // Check if the grid is completely empty after processing matches
            yield return new WaitForSeconds(0.2f);  // Small delay to ensure all animations have completed
            
            // Refresh back layer item positions to ensure visibility
            RefreshBackLayerPositions();
            
            if (IsGridEmpty())
            {
                Debug.Log("GridManager: Grid is now empty! All items have been cleared.");
                
                // Find the SortingManager to check level completion
                SortingManager sortingManager = FindObjectOfType<SortingManager>();
                if (sortingManager != null)
                {
                    Debug.Log("GridManager: Notifying SortingManager that all items are cleared.");
                    sortingManager.CheckAllItemsCleared();
                }
                else
                {
                    Debug.LogError("GridManager: SortingManager not found, cannot trigger level completion.");
                }
                
                // Make sure we notify grid state events
                OnGridFilled?.Invoke();
                OnBoardStable?.Invoke();
            }
            
            _isProcessingMatches = false;
        }
        
        // Modified method to check for empty rows within sections and promote back items in those rows
        private void CheckAndPromoteEmptySections()
        {
            int sectionsCount = NumberOfSections;
            int columnsPerSection = ColumnsPerSection;
            
            // Debug.Log($"Checking all {sectionsCount} sections, row by row (columns per section: {columnsPerSection})");
            
            // Check each section
            for (int section = 0; section < sectionsCount; section++)
            {
                // Skip disabled sections
                if (_disabledSections.Contains(section))
                {
                    // Debug.Log($"Skipping disabled section {section}");
                    continue;
                }
                
                // Calculate section bounds
                int startX = section * columnsPerSection;
                int endX = Mathf.Min((section + 1) * columnsPerSection - 1, _gridWidth - 1);
                
                // Debug.Log($"Checking section {section} from column {startX} to {endX}");
                
                // Check each row within this section
                for (int y = 0; y < _gridHeight; y++)
                {
                    bool isRowEmptyInFront = true;
                    
                    // Check if this row in this section is empty in the front layer
                    for (int x = startX; x <= endX && isRowEmptyInFront; x++)
                    {
                        // If we find any front item in this row of this section, the row is not empty
                        if (_grid[x, y] != null)
                        {
                            isRowEmptyInFront = false;
                            // Debug.Log($"Found front item at ({x},{y}) in section {section}, row {y}");
                        }
                    }
                    
                    // Debug.Log($"Section {section}, Row {y} is {(isRowEmptyInFront ? "empty" : "not empty")} in front layer");
                    
                    // If this row in this section is empty in the front layer, promote back items in this row only
                    if (isRowEmptyInFront)
                    {
                        Debug.Log($"Section {section}, Row {y} is empty in front layer, checking for back items to promote");
                        
                        // Check if there are any back items to promote in this row
                        List<Vector2Int> backItemPositions = new List<Vector2Int>();
                        
                        // First, collect all back items in this row of this section
                        for (int x = startX; x <= endX; x++)
                        {
                            if (_backGrid[x, y] != null)
                            {
                                backItemPositions.Add(new Vector2Int(x, y));
                            }
                        }
                        
                        // Only promote if we found back items
                        if (backItemPositions.Count > 0)
                        {
                            Debug.Log($"Found {backItemPositions.Count} back items in section {section}, row {y}, promoting them to front");
                            
                            // Promote all back items in this row of this section together
                            foreach (Vector2Int pos in backItemPositions)
                            {
                                Debug.Log($"Promoting back item at ({pos.x},{pos.y}) in section {section}, row {y}");
                                PromoteBackItemToFront(pos.x, pos.y);
                            }
                        }
                        else
                        {
                            Debug.Log($"No back items found in section {section}, row {y}");
                        }
                    }
                }
            }
        }
        
        // Existing promotion method for individual positions
        private void PromoteBackItemToFront(int x, int y)
        {
            // Check if there's a back item at this position
            if (_backGrid[x, y] != null)
            {
                Debug.Log($"Promoting back item at ({x},{y}) to front layer");
                
                // Get the item from the back grid
                GridItem backItem = _backGrid[x, y];
                
                // Move the item from back to front grid
                _grid[x, y] = backItem;
                _backGrid[x, y] = null;
                
                // Reset the proper position (remove any offset)
                Vector3 position = new Vector3(
                    (x + 0.5f) * _cellSize, 
                    (y + 0.5f) * _cellSize, 
                    -0.1f  // Front layer z position
                );
                
                // Set position immediately before promotion animation
                backItem.transform.position = position;
                
                // Update the item's layer status
                backItem.PromoteToFrontLayer();
            }
        }
        
        private List<GridItem> FindHorizontalMatchAt(int x, int y)
        {
            List<GridItem> result = new List<GridItem>();
            GridItem current = _grid[x, y];
            
            if (current == null) return result;
            
            int itemType = current.ItemType;
            result.Add(current);
            
            // Check left
            int leftBoundary = (x / (GridWidth / NumberOfSections)) * (GridWidth / NumberOfSections);
            for (int i = x - 1; i >= leftBoundary; i--)
            {
                GridItem item = _grid[i, y];
                if (item != null && item.ItemType == itemType)
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }
            
            // Check right
            int rightBoundary = leftBoundary + (GridWidth / NumberOfSections) - 1;
            for (int i = x + 1; i <= rightBoundary; i++)
            {
                GridItem item = _grid[i, y];
                if (item != null && item.ItemType == itemType)
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }
            
            // Only return if we have at least 3 items and all are in the same section
            if (result.Count >= 3)
            {
                return result;
            }
            
            return new List<GridItem>();
        }
        
        private List<GridItem> FindVerticalMatchAt(int x, int y)
        {
            List<GridItem> result = new List<GridItem>();
            GridItem current = _grid[x, y];
            
            if (current == null) return result;
            
            int itemType = current.ItemType;
            result.Add(current);
            
            // Check down
            for (int i = y - 1; i >= 0; i--)
            {
                GridItem item = _grid[x, i];
                if (item != null && item.ItemType == itemType)
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }
            
            // Check up
            for (int i = y + 1; i < _gridHeight; i++)
            {
                GridItem item = _grid[x, i];
                if (item != null && item.ItemType == itemType)
                {
                    result.Add(item);
                }
                else
                {
                    break;
                }
            }
            
            // Only return if we have at least 3 items
            if (result.Count >= 3)
            {
                return result;
            }
            
            return new List<GridItem>();
        }
        
        private IEnumerator FillEmptySpaces()
        {
            _isFillingBoard = true;
            
            // In the new system, we don't fill empty spaces with new items
            // or have items fall down to lower positions
            
            // Just perform cleanup to ensure grid consistency
            CleanupOrphanedGridItems();
            
            _isFillingBoard = false;
            yield return null;
        }
        
        // New method to cleanup any orphaned or duplicate items
        private void CleanupOrphanedGridItems()
        {
            // First, create a set of all valid grid items
            HashSet<GridItem> validItems = new HashSet<GridItem>();
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y] != null)
                    {
                        validItems.Add(_grid[x, y]);
                    }
                }
            }
            
            // Find all GridItem components under the grid container
            GridItem[] allItems = _gridContainer.GetComponentsInChildren<GridItem>();
            
            // Destroy any that aren't in our valid set
            foreach (GridItem item in allItems)
            {
                if (!validItems.Contains(item))
                {
                    Debug.LogWarning($"Found orphaned grid item {item.name} at position ({item.GridX},{item.GridY}) - destroying");
                    Destroy(item.gameObject);
                }
            }
        }
        
        private void OnDisable()
        {
            // Reset state when the grid is disabled (scene change or game exit)
            ResetSelectionState();
            
            // Reset flags
            _isSwapping = false;
            _isProcessingMatches = false;
            _isFillingBoard = false;
            
            // Clean up any running coroutines
            StopAllCoroutines();
            
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            
            // Unsubscribe from level completion and failure events
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.OnLevelCompleted -= HandleLevelCompleted;
                levelManager.OnLevelFailed -= HandleLevelFailed;
            }
            
            Debug.Log("GridManager OnDisable: Resetting game state and unsubscribing from events");
        }
        
        private void OnEnable()
        {
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            
            // Subscribe to level completion and failure events
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.OnLevelCompleted += HandleLevelCompleted;
                levelManager.OnLevelFailed += HandleLevelFailed;
            }
        }
        
        // Hide grid when level is completed
        private void HandleLevelCompleted()
        {
            if (_gridContainer != null)
            {
                _gridContainer.gameObject.SetActive(false);
                Debug.Log("GridManager: Hiding grid for level completion");
            }
        }
        
        // Hide grid when level is failed
        private void HandleLevelFailed()
        {
            if (_gridContainer != null)
            {
                _gridContainer.gameObject.SetActive(false);
                Debug.Log("GridManager: Hiding grid for level failure");
            }
        }
        
        // Public method to force a complete reset of the grid
        public void ForceReset()
        {
            Debug.Log("GridManager: ForceReset called");
            
            // Stop all animations
            StopAllCoroutines();
            
            // Reset state flags
            _isSwapping = false;
            _isProcessingMatches = false;
            _isFillingBoard = false;
            
            // No longer hardcoding dimensions - will use dimensions from LevelData
            
            // Clear all items
            ClearGrid();
            
            // Check current game state and only show grid in gameplay
            GameManager.GameState currentState = GameManager.Instance?.GetCurrentState() ?? GameManager.GameState.MainMenu;
            
            if (currentState == GameManager.GameState.Gameplay)
            {
                // Show and initialize grid in gameplay state
                if (_gridContainer != null)
                {
                    _gridContainer.gameObject.SetActive(true);
                }
                InitializeGrid(); // This now includes CenterGridContainer
                Debug.Log("GridManager: Grid reset and initialized for gameplay");
            }
            else
            {
                // Keep grid hidden in menu states
                if (_gridContainer != null)
                {
                    _gridContainer.gameObject.SetActive(false);
                }
                Debug.Log("GridManager: Grid reset but kept hidden (in menu state)");
            }
        }
        
        // New method to allow LevelManager to stop coroutines
        public new void StopAllCoroutines()
        {
            base.StopAllCoroutines();
            Debug.Log("GridManager: Stopped all coroutines");
        }
        
        // Add a new method to reset selection state
        public void ResetSelectionState()
        {
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(false);
                _selectedItem = null;
                
                // Reset any dragging state
                _isDragging = false;
                
                // Fire event for visualization
                OnSelectionCleared?.Invoke();
            }
        }
        
        // Public helper to check if a cell is empty
        public bool IsCellEmpty(int x, int y)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return false; // Out of bounds is not considered empty
                
            return _grid[x, y] == null;
        }
        
        // Add method to check if there's a selected item
        public bool HasSelectedItem()
        {
            return _selectedItem != null;
        }
        
        // Get the currently selected item
        public GridItem GetSelectedItem()
        {
            return _selectedItem;
        }
        
        // Get the selected position
        public Vector2Int GetSelectedPosition()
        {
            return _selectedPosition;
        }
        
        // Add method to check if two positions are in the same section
        public bool AreInSameSection(int x1, int x2)
        {
            int columnsPerSection = _gridWidth / _numberOfSections;
            int section1 = x1 / columnsPerSection;
            int section2 = x2 / columnsPerSection;
            return section1 == section2;
        }
        
        // Get the section index for a given x coordinate
        public int GetSectionForColumn(int x)
        {
            int columnsPerSection = _gridWidth / _numberOfSections;
            return x / columnsPerSection;
        }
        
        // Improved method to create visual separators between sections
        private void CreateSectionSeparators()
        {
            // Remove any existing separators first
            foreach (Transform child in _gridContainer)
            {
                if (child.name.Contains("SectionSeparator") || child.name.Contains("ShelfBackground") || 
                    child.name.Contains("VerticalSeparator") || child.name.Contains("HorizontalSeparator") ||
                    child.name.Contains("ShelfLeg"))
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Create the separator prefab if needed
            if (_sectionSeparatorPrefab == null)
            {
                // Create a default separator if none is provided
                _sectionSeparatorPrefab = new GameObject("DefaultSectionSeparator");
                SpriteRenderer renderer = _sectionSeparatorPrefab.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateSeparatorSprite();
                renderer.color = _sectionSeparatorColor;
                
                // Hide the prefab itself from the scene by making its renderer disabled
                // This keeps the GameObject active but not visible
                renderer.enabled = false;
                
                // Move it outside the visible area as an extra precaution
                _sectionSeparatorPrefab.transform.position = new Vector3(-1000f, -1000f, -1000f);
                
                // Don't parent it to anything in the scene
                DontDestroyOnLoad(_sectionSeparatorPrefab);
            }
            
            // Create shelf backgrounds only for enabled sections
            for (int row = 0; row < _gridHeight; row++)
            {
                // For each section in this row
                for (int section = 0; section < _numberOfSections; section++)
                {
                    // Skip disabled sections
                    if (_disabledSections.Contains(section))
                    {
                        continue;
                    }
                    
                    // Calculate section bounds
                    int startX = section * (_gridWidth / _numberOfSections);
                    int endX = startX + (_gridWidth / _numberOfSections) - 1;
                    
                    // Create a background panel for this section in this row
                    GameObject background = new GameObject($"ShelfBackground_Section{section}_Row{row}");
                    background.transform.SetParent(_gridContainer);
                    
                    // Position the background to cover just this section of the row
                    float xPos = (startX + (endX - startX) / 2.0f) * _cellSize + (_cellSize / 2.0f);
                    float yPos = (row + 0.5f) * _cellSize;
                    
                    // Position background far back to ensure it doesn't interfere with items
                    background.transform.position = new Vector3(xPos, yPos, 1.0f);
                    
                    // Add a sprite renderer
                    SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
                    renderer.sprite = CreateBackgroundSprite();
                    
                    // Scale to cover just this section of the row
                    float height = _cellSize * 1.0f;
                    float width = (_gridWidth / _numberOfSections) * _cellSize;
                    background.transform.localScale = new Vector3(
                        width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y,
                        1f
                    );
                    
                    // Very subtle background
                    renderer.color = new Color(0.92f, 0.88f, 0.82f, 0.25f);
                    // Very low sorting order to ensure it's behind grid items
                    renderer.sortingOrder = -10;
                }
            }
            
            int columnsPerSection = _gridWidth / _numberOfSections;
            
            // Track which columns should have vertical separators
            bool[] shouldHaveVerticalSeparator = new bool[_gridWidth + 1];
            
            // The leftmost and rightmost columns always have separators
            shouldHaveVerticalSeparator[0] = true;
            shouldHaveVerticalSeparator[_gridWidth] = true;
            
            // For interior separators between sections, only add if at least one adjacent section is enabled
            for (int section = 1; section < _numberOfSections; section++)
            {
                int x = section * columnsPerSection;
                bool leftSectionEnabled = !_disabledSections.Contains(section - 1);
                bool rightSectionEnabled = !_disabledSections.Contains(section);
                
                // Only add separator if at least one adjacent section is enabled
                shouldHaveVerticalSeparator[x] = leftSectionEnabled || rightSectionEnabled;
            }
            
            // Create a shelf structure with legs at each end, but only at visible edges
            // Left leg (only if leftmost section is enabled)
            if (!_disabledSections.Contains(0))
            {
                GameObject leftLeg = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                leftLeg.name = "ShelfLeg_Left";
                leftLeg.transform.position = new Vector3(0f, 0f, 0f);
                leftLeg.GetComponent<SpriteRenderer>().enabled = true;
                
                // Scale the leg
                SpriteRenderer renderer = leftLeg.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    float width = 0.35f;
                    float height = 0.8f;
                    leftLeg.transform.localScale = new Vector3(
                        width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y,
                        1f
                    );
                    renderer.sortingOrder = 15;
                    renderer.color = new Color(0.65f, 0.4f, 0.25f, 1.0f);
                }
            }
            
            // Right leg (only if rightmost section is enabled)
            if (!_disabledSections.Contains(_numberOfSections - 1))
            {
                GameObject rightLeg = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                rightLeg.name = "ShelfLeg_Right";
                rightLeg.transform.position = new Vector3(_gridWidth * _cellSize, 0f, 0f);
                rightLeg.GetComponent<SpriteRenderer>().enabled = true;
                
                // Scale the leg
                SpriteRenderer renderer = rightLeg.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    float width = 0.35f;
                    float height = 0.8f;
                    rightLeg.transform.localScale = new Vector3(
                        width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y,
                        1f
                    );
                    renderer.sortingOrder = 15;
                    renderer.color = new Color(0.65f, 0.4f, 0.25f, 1.0f);
                }
            }
            
            // Create vertical separators only where needed
            for (int x = 0; x <= _gridWidth; x++)
            {
                if (shouldHaveVerticalSeparator[x])
                {
                    GameObject vertSeparator = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                    vertSeparator.name = $"VerticalSeparator_{x}";
                    vertSeparator.transform.position = new Vector3(x * _cellSize, (_gridHeight * 0.5f) * _cellSize, 0f);
                    vertSeparator.GetComponent<SpriteRenderer>().enabled = true;
                    
                    // Scale the separator
                    SpriteRenderer renderer = vertSeparator.GetComponent<SpriteRenderer>();
                    if (renderer != null && renderer.sprite != null)
                    {
                        float width = 0.2f;
                        float height = _gridHeight * _cellSize;
                        vertSeparator.transform.localScale = new Vector3(
                            width / renderer.sprite.bounds.size.x,
                            height / renderer.sprite.bounds.size.y,
                            1f
                        );
                        renderer.sortingOrder = 8;
                        renderer.color = new Color(0.8f, 0.6f, 0.4f, 1.0f);
                    }
                }
            }
            
            // For horizontal separators, create continuous separators only spanning enabled sections
            List<int> enabledSections = new List<int>();
            for (int section = 0; section < _numberOfSections; section++)
            {
                if (!_disabledSections.Contains(section))
                {
                    enabledSections.Add(section);
                }
            }
            
            // If no sections are enabled, we're done
            if (enabledSections.Count == 0)
            {
                return;
            }
            
            // Create bottom horizontal separator covering all enabled sections
            for (int i = 0; i < enabledSections.Count; i++)
            {
                int section = enabledSections[i];
                int startX = section * columnsPerSection;
                int endX = (section + 1) * columnsPerSection;
                
                // Find consecutive enabled sections
                int lastSection = section;
                while (i + 1 < enabledSections.Count && enabledSections[i + 1] == lastSection + 1)
                {
                    i++;
                    lastSection = enabledSections[i];
                    endX = (lastSection + 1) * columnsPerSection;
                }
                
                // Create bottom edge for this range of sections
                GameObject bottomEdge = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                bottomEdge.name = $"HorizontalSeparator_Bottom_{startX}_to_{endX}";
                float xCenter = (startX + endX) * 0.5f * _cellSize;
                bottomEdge.transform.position = new Vector3(xCenter, 0f, 0f);
                bottomEdge.GetComponent<SpriteRenderer>().enabled = true;
                
                // Scale the edge
                SpriteRenderer renderer = bottomEdge.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    float height = 0.3f;
                    float width = (endX - startX) * _cellSize;
                    bottomEdge.transform.localScale = new Vector3(
                        width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y,
                        1f
                    );
                    renderer.sortingOrder = 12;
                    renderer.color = new Color(0.7f, 0.5f, 0.3f, 1.0f);
                }
                
                // Create top edge for this range of sections
                GameObject topEdge = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                topEdge.name = $"HorizontalSeparator_Top_{startX}_to_{endX}";
                topEdge.transform.position = new Vector3(xCenter, _gridHeight * _cellSize, 0f);
                topEdge.GetComponent<SpriteRenderer>().enabled = true;
                
                // Scale the edge
                renderer = topEdge.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    float height = 0.3f;
                    float width = (endX - startX) * _cellSize;
                    topEdge.transform.localScale = new Vector3(
                        width / renderer.sprite.bounds.size.x,
                        height / renderer.sprite.bounds.size.y,
                        1f
                    );
                    renderer.sortingOrder = 12;
                    renderer.color = new Color(0.7f, 0.5f, 0.3f, 1.0f);
                }
                
                // Create interior horizontal separators between rows for this section range
                for (int row = 1; row < _gridHeight; row++)
                {
                    GameObject separator = Instantiate(_sectionSeparatorPrefab, _gridContainer);
                    separator.name = $"HorizontalSeparator_Row{row}_{startX}_to_{endX}";
                    float yPos = row * _cellSize;
                    separator.transform.position = new Vector3(xCenter, yPos, 0f);
                    separator.GetComponent<SpriteRenderer>().enabled = true;
                    
                    // Scale the separator
                    renderer = separator.GetComponent<SpriteRenderer>();
                    if (renderer != null && renderer.sprite != null)
                    {
                        float height = 0.25f;
                        float width = (endX - startX) * _cellSize;
                        separator.transform.localScale = new Vector3(
                            width / renderer.sprite.bounds.size.x,
                            height / renderer.sprite.bounds.size.y,
                            1f
                        );
                        renderer.sortingOrder = 12;
                        renderer.color = new Color(0.7f, 0.5f, 0.3f, 1.0f);
                    }
                }
            }
            
            Debug.Log($"Created shelf structure for grid with {enabledSections.Count} enabled sections");
        }
        
        // Create a sprite for the separator with a stronger, more visible color
        private Sprite CreateSeparatorSprite()
        {
            // Create a larger texture for better quality when scaling
            Texture2D texture = new Texture2D(16, 16);
            Color[] colors = new Color[256];
            
            // Create a wood-like pattern with some variations
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    // Base wood color
                    Color woodColor = new Color(0.82f, 0.62f, 0.42f, 1.0f);
                    
                    // Add some variations to create a wood-like pattern
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.15f;
                    woodColor += new Color(noise, noise, noise, 0);
                    
                    // Add darker grain lines occasionally
                    if ((y + 5) % 7 == 0 || (x + 3) % 9 == 0)
                    {
                        woodColor *= 0.85f;
                    }
                    
                    colors[y * 16 + x] = woodColor;
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        // Create a sprite for the shelf background
        private Sprite CreateBackgroundSprite()
        {
            // Create a texture for the background
            Texture2D texture = new Texture2D(16, 16);
            Color[] colors = new Color[256];
            
            // Create a very subtle background pattern - almost uniform
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    // Very light tan color as base - this will be barely visible
                    Color bgColor = new Color(0.92f, 0.90f, 0.85f, 1.0f);
                    
                    // Add extremely subtle noise (reduced by 50%)
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.02f;
                    bgColor += new Color(noise, noise, noise, 0);
                    
                    colors[y * 16 + x] = bgColor;
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        // Modified method to get the item at a specific position
        public GridItem GetItemAt(int x, int y, bool getBackItem = false)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return null;
                
            return getBackItem ? _backGrid[x, y] : _grid[x, y];
        }
        
        // Process a custom match (for powers)
        public IEnumerator ProcessCustomMatch(List<GridItem> itemsToMatch)
        {
            if (itemsToMatch == null || itemsToMatch.Count < 3)
            {
                Debug.LogWarning("Cannot process custom match: not enough items");
                yield break;
            }
            
            _isProcessingMatches = true;
            
            // Get the item type for tracking
            int itemType = itemsToMatch[0].ItemType;
            
            // Animate the match
            foreach (GridItem item in itemsToMatch)
            {
                // Mark position as empty
                _grid[item.GridX, item.GridY] = null;
                
                // Animate item disappearing
                item.PlayMatchAnimation();
            }
            
            // Wait for animation
            yield return new WaitForSeconds(0.3f);
            
            // Destroy matched items
            foreach (GridItem item in itemsToMatch)
            {
                Destroy(item.gameObject);
            }
            
            // Notify listeners of the matched items
            OnItemsMatched?.Invoke(itemType, itemsToMatch);
            
            // Wait for stability
            yield return new WaitForSeconds(0.2f);
            
            // Check for empty sections to promote back layer items
            CheckAndPromoteEmptySections();
            
            // Wait for promotion animations
            yield return new WaitForSeconds(0.3f);
            
            // Check for matches
            yield return StartCoroutine(CheckForMatchesCoroutine());
            
            _isProcessingMatches = false;
            
            // Notify that the board is stable
            OnBoardStable?.Invoke();
        }
        
        // Shuffle the grid items
        public IEnumerator ShuffleGrid()
        {
            _isFillingBoard = true;
            
            // Collect all items
            List<GridItem> allItems = new List<GridItem>();
            int totalItems = 0;
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y] != null)
                    {
                        allItems.Add(_grid[x, y]);
                        totalItems++;
                    }
                }
            }
            
            // Shuffle the list
            for (int i = 0; i < allItems.Count; i++)
            {
                GridItem temp = allItems[i];
                int randomIndex = UnityEngine.Random.Range(i, allItems.Count);
                allItems[i] = allItems[randomIndex];
                allItems[randomIndex] = temp;
            }
            
            // Clear grid references
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _grid[x, y] = null;
                }
            }
            
            // Create a list of all possible grid positions
            List<Vector2Int> allPositions = new List<Vector2Int>();
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    allPositions.Add(new Vector2Int(x, y));
                }
            }
            
            // Shuffle all positions
            for (int i = 0; i < allPositions.Count; i++)
            {
                Vector2Int temp = allPositions[i];
                int randomIndex = UnityEngine.Random.Range(i, allPositions.Count);
                allPositions[i] = allPositions[randomIndex];
                allPositions[randomIndex] = temp;
            }
            
            // Place items back in grid at random positions
            for (int i = 0; i < allItems.Count; i++)
            {
                if (i < allPositions.Count)
                {
                    GridItem item = allItems[i];
                    Vector2Int newPos = allPositions[i];
                    
                    // Calculate new position
                    Vector3 newPosition = new Vector3(
                        (newPos.x + 0.5f) * _cellSize,
                        (newPos.y + 0.5f) * _cellSize,
                        0
                    );
                    
                    // Animate to new position
                    StartCoroutine(AnimateItemToPosition(item, newPosition));
                    
                    // Update grid and item references
                    _grid[newPos.x, newPos.y] = item;
                    item.SetGridPosition(newPos.x, newPos.y);
                }
            }
            
            // Wait for animations to complete
            yield return new WaitForSeconds(0.5f);
            
            // Check for empty sections to promote back layer items
            CheckAndPromoteEmptySections();
            
            // Wait for promotion animations
            yield return new WaitForSeconds(0.3f);
            
            // Check for matches
            yield return StartCoroutine(CheckForMatchesCoroutine());
            
            _isFillingBoard = false;
            
            // Notify that the board is stable
            OnBoardStable?.Invoke();
        }
        
        private IEnumerator AnimateItemToPosition(GridItem item, Vector3 targetPosition)
        {
            float elapsedTime = 0f;
            Vector3 startPosition = item.transform.position;
            
            // Ensure z position is maintained for proper depth
            targetPosition.z = -0.1f;
            
            // Ensure item stays in front during animation
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 15; // Higher than separators (10) during animation
            }
            
            while (elapsedTime < _swapSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / _swapSpeed);
                
                item.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                
                yield return null;
            }
            
            // Ensure final position is exact
            item.transform.position = targetPosition;
            
            // Reset sorting order back to normal
            if (renderer != null)
            {
                renderer.sortingOrder = 5;
            }
        }
        
        // Add method to check if item is being dragged
        public bool IsDragging()
        {
            return _isDragging;
        }
        
        // Compatibility method for InputHandler.cs
        // This provides backward compatibility for code still calling the old selection method
        public bool TrySelectItem(int x, int y)
        {
            // Validation checks
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return false;
            
            if (_isSwapping || _isProcessingMatches || _isFillingBoard)
            {
                Debug.Log($"Cannot select: isSwapping={_isSwapping}, isProcessingMatches={_isProcessingMatches}, isFillingBoard={_isFillingBoard}");
                return false;
            }
            
            // If dragging, don't allow new selections
            if (_isDragging)
                return false;
                
            // First selection - must select an item (not an empty space)
            if (_selectedItem == null)
            {
                GridItem clickedItem = _grid[x, y];
                if (clickedItem == null) 
                {
                    Debug.Log("First selection must be an item, not an empty space.");
                    return false; // Cannot select empty space as first selection
                }
                
                _selectedItem = clickedItem;
                _selectedPosition = new Vector2Int(x, y);
                _selectedItem.SetSelected(true);
                
                // Move item to front during selection
                SpriteRenderer renderer = _selectedItem.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 15; // Higher than separators during selection
                }
                
                Debug.Log($"Selected item at ({x}, {y})");
                
                // Fire event for visualization
                OnItemSelected?.Invoke(x, y);
                
                return true;
            }
            // Second selection - must select an empty space
            else
            {
                // Check if selecting the same item again (deselect)
                if (x == _selectedPosition.x && y == _selectedPosition.y)
                {
                    _selectedItem.SetSelected(false);
                    
                    // Reset sorting order
                    SpriteRenderer renderer = _selectedItem.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 5; // Back to normal ordering
                    }
                    
                    _selectedItem = null;
                    Debug.Log("Deselected item");
                    
                    // Fire event for visualization
                    OnSelectionCleared?.Invoke();
                    
                    return true;
                }
                
                // For second selection, player must select an empty space
                if (_grid[x, y] != null)
                {
                    // If it's another item, switch selection
                    _selectedItem.SetSelected(false);
                    
                    // Reset sorting order of previously selected item
                    SpriteRenderer prevRenderer = _selectedItem.GetComponent<SpriteRenderer>();
                    if (prevRenderer != null)
                    {
                        prevRenderer.sortingOrder = 5;
                    }
                    
                    _selectedItem = _grid[x, y];
                    _selectedPosition = new Vector2Int(x, y);
                    _selectedItem.SetSelected(true);
                    
                    // Move new item to front during selection
                    SpriteRenderer newRenderer = _selectedItem.GetComponent<SpriteRenderer>();
                    if (newRenderer != null)
                    {
                        newRenderer.sortingOrder = 15;
                    }
                    
                    Debug.Log($"Switched selection to item at ({x}, {y})");
                    
                    // Fire event for visualization
                    OnItemSelected?.Invoke(x, y);
                    
                    return true;
                }
                
                // Empty space - we can now move to ANY empty space, not just adjacent ones
                // Move item to empty space
                StartCoroutine(SwapWithEmpty(_selectedPosition.x, _selectedPosition.y, x, y));
                _selectedItem.SetSelected(false);
                _selectedItem = null;
                Debug.Log($"Moving item from ({_selectedPosition.x}, {_selectedPosition.y}) to ({x}, {y})");
                
                // Fire event for visualization
                OnSelectionCleared?.Invoke();
                
                return true;
            }
        }
        
        // Add new method to refresh back layer positions
        private void RefreshBackLayerPositions()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_backGrid[x, y] != null)
                    {
                        Vector3 position = new Vector3(
                            (x + 0.5f) * _cellSize, 
                            (y + 0.5f) * _cellSize, 
                            0.2f  // Back layer z position
                        );
                        
                        // Check if there's a front item at this position
                        bool hasItemInFront = _grid[x, y] != null;
                        _backGrid[x, y].PositionWithOffset(position, hasItemInFront);
                    }
                }
            }
        }
    }
} 