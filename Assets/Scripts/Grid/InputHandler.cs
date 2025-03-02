using UnityEngine;
using GoodsSorting.Managers;
using System.Collections.Generic;

namespace GoodsSorting.Grid
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private bool _debugInput = false;
        
        private bool _isInputEnabled = true;
        private Vector2Int _currentHoverCell = new Vector2Int(-1, -1);
        private Dictionary<Vector2Int, GameObject> _emptySpaceHighlights = new Dictionary<Vector2Int, GameObject>();
        
        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }
        
        private void Start()
        {
            // Subscribe to game manager events to handle pausing
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += HandleGameStateChanged;
            }
            
            // Initialize with correct state
            _isInputEnabled = (GameManager.Instance?.GetCurrentState() == GameManager.GameState.Gameplay);
            
            // Subscribe to grid events for highlighting
            if (_gridManager != null)
            {
                _gridManager.OnGridFilled += HighlightEmptySpaces;
                _gridManager.OnBoardStable += HighlightEmptySpaces;
                
                // Initial highlight of empty spaces
                HighlightEmptySpaces();
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            
            if (_gridManager != null)
            {
                _gridManager.OnGridFilled -= HighlightEmptySpaces;
                _gridManager.OnBoardStable -= HighlightEmptySpaces;
            }
            
            ClearEmptySpaceHighlights();
        }
        
        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            // Only enable input during gameplay
            _isInputEnabled = (newState == GameManager.GameState.Gameplay);
            
            // Clear highlights when leaving gameplay
            if (!_isInputEnabled)
            {
                ClearEmptySpaceHighlights();
            }
            else if (newState == GameManager.GameState.Gameplay)
            {
                // If entering gameplay, refresh highlights
                HighlightEmptySpaces();
            }
        }
        
        // Helper method to get the world position for a grid cell
        private Vector3 GetCellWorldPosition(int gridX, int gridY)
        {
            float cellSize = _gridManager.CellSize;
            
            // Get the grid container's position to account for any offset
            Vector3 gridOffset = Vector3.zero;
            Transform gridContainer = _gridManager.transform.Find("GridContainer");
            if (gridContainer != null)
            {
                gridOffset = gridContainer.position;
            }
            
            // Direct snap: Find the actual item at this position if it exists
            if (gridContainer != null)
            {
                foreach (Transform child in gridContainer)
                {
                    GridItem item = child.GetComponent<GridItem>();
                    if (item != null && item.GridX == gridX && item.GridY == gridY)
                    {
                        // Use the item's actual position rather than calculating from grid coords
                        return child.position;
                    }
                }
            }
            
            // No item found, calculate position based on the grid - center it in the cell
            return new Vector3(
                gridOffset.x + (gridX + 0.5f) * cellSize,
                gridOffset.y + (gridY + 0.5f) * cellSize,
                0
            );
        }
        
        private void Update()
        {
            if (!_isInputEnabled) return;
            
            // Convert mouse position to grid coordinates
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // Ensure we're on the same z-plane as the grid
            
            // Get grid container position for offset
            Vector3 gridOffset = Vector3.zero;
            Transform gridContainer = _gridManager.transform.Find("GridContainer");
            if (gridContainer != null)
            {
                gridOffset = gridContainer.position;
            }
            
            // Adjust mouse position by grid offset
            Vector3 relativePos = mouseWorldPos - gridOffset;
            
            float cellSize = _gridManager.CellSize;
            int gridX = Mathf.FloorToInt(relativePos.x / cellSize);
            int gridY = Mathf.FloorToInt(relativePos.y / cellSize);
            
            // Debug log grid dimensions and mouse position
            if (_debugInput && Input.GetMouseButtonDown(0))
            {
                Debug.Log($"Grid dimensions: {_gridManager.GridWidth}x{_gridManager.GridHeight}");
                Debug.Log($"Mouse world position: {mouseWorldPos}, Relative position: {relativePos}");
                Debug.Log($"Calculated grid position: ({gridX}, {gridY})");
            }
            
            // Check if the mouse is over a valid grid cell
            bool isValidCell = gridX >= 0 && gridX < _gridManager.GridWidth && 
                               gridY >= 0 && gridY < _gridManager.GridHeight;
            
            // Update current hovered cell
            Vector2Int newHoverCell = isValidCell ? new Vector2Int(gridX, gridY) : new Vector2Int(-1, -1);
            if (newHoverCell != _currentHoverCell)
            {
                _currentHoverCell = newHoverCell;
                
                if (_debugInput && isValidCell)
                {
                    Debug.Log($"Mouse over grid cell: ({gridX}, {gridY})");
                }
            }
            
            // Process mouse click for item selection
            if (Input.GetMouseButtonDown(0) && isValidCell)
            {
                if (_debugInput)
                {
                    Debug.Log($"Click on grid cell: ({gridX}, {gridY})");
                }
                
                // Improved accuracy: Using a raycast to detect grid items directly
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0.1f, LayerMask.GetMask("Grid"));
                if (hit.collider != null)
                {
                    // Found an item directly under the mouse click
                    GridItem hitItem = hit.collider.GetComponent<GridItem>();
                    if (hitItem != null)
                    {
                        gridX = hitItem.GridX;
                        gridY = hitItem.GridY;
                        
                        if (_debugInput)
                        {
                            Debug.Log($"Hit grid item at: ({gridX}, {gridY})");
                        }
                    }
                }
                
                // Try to select an item at the clicked position
                bool selectionResult = _gridManager.TrySelectItem(gridX, gridY);
                
                // After selection, update empty space highlights as some might have become valid/invalid
                HighlightEmptySpaces();
            }
        }
        
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            
            // Clear highlights when input is disabled
            if (!enabled)
            {
                ClearEmptySpaceHighlights();
            }
        }
        
        // Method to highlight empty spaces
        public void HighlightEmptySpaces()
        {
            // Completely disabled empty space highlighting as requested
            ClearEmptySpaceHighlights();
            
            // No new highlights will be created
        }
        
        private GameObject CreateHighlight(Vector3 position, float size, Color color)
        {
            GameObject highlight = new GameObject("EmptySpaceHighlight");
            highlight.transform.position = position;
            highlight.transform.SetParent(transform);
            
            // Add a collider to make empty spaces clickable like items
            BoxCollider2D collider = highlight.AddComponent<BoxCollider2D>();
            
            // Make the collider cover the entire cell (100% of cell size)
            collider.size = new Vector2(size, size);
            
            // Set the same layer as grid items for consistent raycasting
            int gridLayer = LayerMask.NameToLayer("Grid");
            if (gridLayer != -1) // Only set if the layer exists
            {
                highlight.layer = gridLayer;
            }
            
            // Add a GridEmptySpace component to help identify this as an empty space
            GridEmptySpace emptySpace = highlight.AddComponent<GridEmptySpace>();
            
            // Set the grid coordinates so we can retrieve them when clicked
            Vector3 gridOffset = Vector3.zero;
            Transform gridContainer = _gridManager.transform.Find("GridContainer");
            if (gridContainer != null)
            {
                gridOffset = gridContainer.position;
            }
            
            // Calculate grid position more accurately using the center-based position
            float cellSize = _gridManager.CellSize;
            Vector3 adjustedPos = new Vector3(position.x - gridOffset.x, position.y - gridOffset.y, 0);
            int gridX = Mathf.RoundToInt(adjustedPos.x / cellSize - 0.5f);
            int gridY = Mathf.RoundToInt(adjustedPos.y / cellSize - 0.5f);
            emptySpace.SetGridPosition(gridX, gridY);
            
            // No visual element - completely invisible but still clickable
            
            return highlight;
        }
        
        private void ClearEmptySpaceHighlights()
        {
            foreach (var highlight in _emptySpaceHighlights.Values)
            {
                Destroy(highlight);
            }
            
            _emptySpaceHighlights.Clear();
        }
        
        private void OnDisable()
        {
            // Clean up all highlights when disabled
            ClearEmptySpaceHighlights();
        }
    }
} 