using UnityEngine;

namespace GoodsSorting.Grid
{
    /// <summary>
    /// Component for empty grid spaces to make them selectable
    /// </summary>
    public class GridEmptySpace : MonoBehaviour
    {
        private int _gridX;
        private int _gridY;
        
        /// <summary>
        /// X position in the grid
        /// </summary>
        public int GridX => _gridX;
        
        /// <summary>
        /// Y position in the grid
        /// </summary>
        public int GridY => _gridY;
        
        /// <summary>
        /// Set the grid position for this empty space
        /// </summary>
        public void SetGridPosition(int x, int y)
        {
            _gridX = x;
            _gridY = y;
        }
    }
} 