// Script updated to force recompile
using System.Collections.Generic;
using UnityEngine;

namespace GoodsSorting.Grid
{
    /// <summary>
    /// Handles visual indicators for the grid to improve user experience
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private Color _emptySpaceColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        [SerializeField] private Color _selectedColor = new Color(0.8f, 0.8f, 0.2f, 0.5f);
        
        // Added section background colors for visual distinction
        [SerializeField] private Color[] _sectionBackgroundColors = new Color[] 
        {
            new Color(0.95f, 0.95f, 0.95f, 0.1f),
            new Color(0.9f, 0.9f, 0.9f, 0.1f),
            new Color(0.85f, 0.85f, 0.85f, 0.1f)
        };
        
        private Dictionary<Vector2Int, GameObject> _cellHighlights = new Dictionary<Vector2Int, GameObject>();
        private GameObject _selectedHighlight;
        private Vector2Int? _selectedPosition;
        
        // Add section background objects
        private List<GameObject> _sectionBackgrounds = new List<GameObject>();
        
        private void Start()
        {
            if (_gridManager == null)
            {
                _gridManager = GetComponent<GridManager>();
                if (_gridManager == null)
                {
                    _gridManager = FindObjectOfType<GridManager>();
                }
            }
            
            if (_gridManager != null)
            {
                // Subscribe to grid events
                _gridManager.OnGridFilled += HighlightEmptySpaces;
                _gridManager.OnItemSelected += HandleItemSelected;
                _gridManager.OnSelectionCleared += HandleSelectionCleared;
                _gridManager.OnBoardStable += HighlightEmptySpaces;
                
                // Create section backgrounds
                CreateSectionBackgrounds();
            }
            else
            {
                Debug.LogError("GridVisualizer: Could not find GridManager reference");
            }
            
            Debug.Log("GridVisualizer started successfully");
        }
        
        private void OnDestroy()
        {
            if (_gridManager != null)
            {
                // Unsubscribe from grid events
                _gridManager.OnGridFilled -= HighlightEmptySpaces;
                _gridManager.OnItemSelected -= HandleItemSelected;
                _gridManager.OnSelectionCleared -= HandleSelectionCleared;
                _gridManager.OnBoardStable -= HighlightEmptySpaces;
            }
            
            // Clean up section backgrounds
            foreach (var bg in _sectionBackgrounds)
            {
                if (bg != null) Destroy(bg);
            }
            _sectionBackgrounds.Clear();
        }
        
        /// <summary>
        /// Create visual indicators for all empty spaces on the grid
        /// </summary>
        public void HighlightEmptySpaces()
        {
            // Clear existing highlights
            ClearHighlights();
            
            if (_gridManager == null) return;
            
            float cellSize = _gridManager.CellSize;
            
            // Find all empty spaces
            for (int x = 0; x < _gridManager.GridWidth; x++)
            {
                for (int y = 0; y < _gridManager.GridHeight; y++)
                {
                    // Check if this cell is empty
                    if (_gridManager.IsCellEmpty(x, y))
                    {
                        // Create a highlight for this empty cell
                        Vector3 position = new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
                        GameObject highlight = CreateHighlight(position, cellSize, _emptySpaceColor);
                        _cellHighlights[new Vector2Int(x, y)] = highlight;
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles when an item is selected in the grid
        /// </summary>
        private void HandleItemSelected(int x, int y)
        {
            // Clear the selected highlight if it exists
            if (_selectedHighlight != null)
            {
                Destroy(_selectedHighlight);
            }
            
            // Create a new highlight at the selected position
            float cellSize = _gridManager.CellSize;
            Vector3 position = new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
            _selectedHighlight = CreateHighlight(position, cellSize, _selectedColor);
            _selectedPosition = new Vector2Int(x, y);
        }
        
        /// <summary>
        /// Handles when selection is cleared
        /// </summary>
        private void HandleSelectionCleared()
        {
            // Clear the selected highlight
            if (_selectedHighlight != null)
            {
                Destroy(_selectedHighlight);
                _selectedHighlight = null;
            }
            
            _selectedPosition = null;
            
            // Update empty space highlights
            HighlightEmptySpaces();
        }
        
        /// <summary>
        /// Creates a highlight GameObject at the specified position
        /// </summary>
        private GameObject CreateHighlight(Vector3 position, float size, Color color)
        {
            GameObject highlight = new GameObject("Highlight");
            highlight.transform.position = position;
            highlight.transform.SetParent(transform);
            
            SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateHighlightSprite();
            renderer.color = color;
            renderer.sortingOrder = 5; // Above grid items
            
            // Scale to match the cell size
            highlight.transform.localScale = new Vector3(size, size, 1f);
            
            return highlight;
        }
        
        /// <summary>
        /// Creates a simple square sprite for the highlight
        /// </summary>
        private Sprite CreateHighlightSprite()
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            
            for (int i = 0; i < colors.Length; i++)
            {
                int x = i % 64;
                int y = i / 64;
                
                // Create a square with thicker borders
                if (x < 3 || x >= 61 || y < 3 || y >= 61)
                {
                    colors[i] = Color.white;
                }
                else
                {
                    colors[i] = new Color(1f, 1f, 1f, 0.2f);
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// Clears all highlight GameObjects
        /// </summary>
        private void ClearHighlights()
        {
            foreach (var highlight in _cellHighlights.Values)
            {
                Destroy(highlight);
            }
            
            _cellHighlights.Clear();
            
            // Keep the selected highlight if there is one
        }
        
        // New method to create background for each section
        private void CreateSectionBackgrounds()
        {
            if (_gridManager == null) return;
            
            // Clear any existing backgrounds
            foreach (var bg in _sectionBackgrounds)
            {
                if (bg != null) Destroy(bg);
            }
            _sectionBackgrounds.Clear();
            
            float cellSize = _gridManager.CellSize;
            int gridWidth = _gridManager.GridWidth;
            int gridHeight = _gridManager.GridHeight;
            int numSections = _gridManager.NumberOfSections;
            int columnsPerSection = gridWidth / numSections;
            
            // Debug.Log($"Creating {numSections} section backgrounds for a {gridWidth}x{gridHeight} grid");
            
            // Create a background for each section
            for (int section = 0; section < numSections; section++)
            {
                // Calculate section boundaries
                int startX = section * columnsPerSection;
                int endX = startX + columnsPerSection - 1;
                
                // Create a background sprite for this section
                GameObject background = new GameObject($"Section{section}Background");
                background.transform.SetParent(transform);
                
                SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateBackgroundSprite();
                
                // Use different colors for alternating sections
                renderer.color = _sectionBackgroundColors[section % _sectionBackgroundColors.Length];
                
                // Position at the center of the section
                // We need to adjust the position to correctly align with the grid cells
                float xCenter = (startX + endX + 1) * 0.5f * cellSize;
                float yCenter = (gridHeight - 1) * 0.5f * cellSize;
                background.transform.position = new Vector3(xCenter, yCenter, 0.2f); // Behind items, in front of grid
                
                // Scale to fit the section size
                float width = columnsPerSection * cellSize;
                float height = gridHeight * cellSize;
                background.transform.localScale = new Vector3(
                    width / renderer.sprite.bounds.size.x,
                    height / renderer.sprite.bounds.size.y,
                    1f
                );
                
                // Sort below items but above grid
                renderer.sortingOrder = -1;
                
                _sectionBackgrounds.Add(background);
            }
            
            // Debug.Log($"Created {numSections} section backgrounds for a {gridWidth}x{gridHeight} grid");
        }
        
        // Helper method to create a sprite for section backgrounds
        private Sprite CreateBackgroundSprite()
        {
            Texture2D texture = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                colors[i] = Color.white;
            }
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
    }
} 