using System.Collections;
using UnityEngine;

namespace GoodsSorting.Grid
{
    public class GridItem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private BoxCollider2D _collider;
        
        private int _itemType;
        private int _gridX;
        private int _gridY;
        private bool _isSelected;
        private Vector3 _originalScale;
        
        // Layer property
        private bool _isBackLayer = false;
        
        // Properties
        public int ItemType => _itemType;
        public int GridX => _gridX;
        public int GridY => _gridY;
        public bool IsSelected => _isSelected;
        public bool IsBackLayer => _isBackLayer;
        
        // Add method to access the sprite renderer
        public SpriteRenderer GetSpriteRenderer()
        {
            return _spriteRenderer;
        }
        
        private void Awake()
        {
            // Store the original scale
            _originalScale = transform.localScale;
            
            // Get required components if not assigned
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (_animator == null)
                _animator = GetComponent<Animator>();
                
            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();
                
            // If still no collider, add one
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider2D>();
                _collider.size = new Vector2(0.8f, 0.8f); // Default size
            }
        }
        
        // Method to set the item type and update visuals
        public void SetItemType(int itemType)
        {
            _itemType = itemType;
            
            // Update visuals based on item type
            // In a real implementation, this would set the appropriate sprite
            // based on the item type from a sprite atlas or array
            
            // For placeholder, we'll just use a color
            switch (itemType)
            {
                case 0:
                    _spriteRenderer.color = Color.red; // Apple
                    break;
                case 1:
                    _spriteRenderer.color = Color.yellow; // Banana
                    break;
                case 2:
                    _spriteRenderer.color = Color.green; // Lime
                    break;
                case 3:
                    _spriteRenderer.color = new Color(1f, 0.5f, 0f); // Orange
                    break;
                case 4:
                    _spriteRenderer.color = new Color(0.5f, 0f, 0.5f); // Grape
                    break;
                case 5:
                    _spriteRenderer.color = new Color(0f, 0.5f, 1f); // Blueberry
                    break;
                default:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }
        
        public void SetGridPosition(int x, int y)
        {
            _gridX = x;
            _gridY = y;
        }
        
        // Method to set the layer status (front/back)
        public void SetBackLayer(bool isBackLayer)
        {
            _isBackLayer = isBackLayer;
            UpdateLayerVisuals();
        }
        
        // Method to update the visual appearance based on the layer
        private void UpdateLayerVisuals()
        {
            if (_isBackLayer)
            {
                // Make item appear as gray in back layer
                _spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray color with 80% opacity
                
                // Disable collider for back layer items
                if (_collider != null)
                {
                    _collider.enabled = false;
                }
                
                // Set back items slightly behind the front items
                Vector3 pos = transform.position;
                pos.z = 0.2f; // Further back than front items
                transform.position = pos;
            }
            else
            {
                // Normal appearance for front layer
                Color layerColor = _spriteRenderer.color;
                layerColor.a = 1.0f; // Full opacity
                _spriteRenderer.color = layerColor;
                
                // Enable collider for front layer items
                if (_collider != null)
                {
                    _collider.enabled = true;
                }
                
                // Set front items in front of back items
                Vector3 pos = transform.position;
                pos.z = -0.1f; // Slightly in front
                transform.position = pos;
            }
        }
        
        // Promote a back layer item to front layer
        public void PromoteToFrontLayer()
        {
            if (_isBackLayer)
            {
                _isBackLayer = false;
                UpdateLayerVisuals();
                
                // Reset color based on the item's type after promotion
                SetItemType(_itemType);
                
                // Play animation to show item coming to front
                StartCoroutine(PromotionEffect());
            }
        }
        
        private IEnumerator PromotionEffect()
        {
            float duration = 0.3f;
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = startScale * 1.3f;
            
            // Scale up
            while (elapsedTime < duration / 2)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / (duration / 2));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Scale back down
            elapsedTime = 0f;
            while (elapsedTime < duration / 2)
            {
                transform.localScale = Vector3.Lerp(targetScale, startScale, elapsedTime / (duration / 2));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end at the exact target scale
            transform.localScale = startScale;
        }
        
        // Set item selection state and update visuals
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            if (selected)
            {
                // Visual indication of selection
                transform.localScale = _originalScale * 1.2f;
                
                // Optional: Add outline effect or other visual indicator
                // _spriteRenderer.material.SetInt("_IsSelected", 1);
                
                // Start a subtle animation if animation controller is configured
                if (_animator != null && _animator.enabled)
                {
                    _animator.SetTrigger("Selected");
                }
            }
            else
            {
                // Reset to original appearance
                transform.localScale = _originalScale;
                
                // Optional: Remove any selection effects
                // _spriteRenderer.material.SetInt("_IsSelected", 0);
                
                if (_animator != null && _animator.enabled)
                {
                    _animator.SetTrigger("Deselected");
                }
            }
        }
        
        // Play effect when item is matched
        public void PlayMatchEffect()
        {
            // Start a coroutine for the match effect
            StartCoroutine(ScaleEffect());
            
            // Could also add particle effects, sound effects, etc.
            
            // If there's an animator, trigger the match animation
            if (_animator != null && _animator.enabled)
            {
                PlayMatchAnimation();
            }
        }
        
        public void PlayMatchAnimation()
        {
            if (_animator != null && _animator.enabled)
            {
                _animator.SetTrigger("Matched");
            }
        }
        
        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
            {
                // Highlight effect - pulse scale
                StartCoroutine(PulseEffect());
                
                // Could also change color, add glow, etc.
                if (_spriteRenderer != null)
                {
                    // Add slight brightness
                    Color originalColor = _spriteRenderer.color;
                    Color brighterColor = new Color(
                        Mathf.Min(1f, originalColor.r * 1.2f),
                        Mathf.Min(1f, originalColor.g * 1.2f),
                        Mathf.Min(1f, originalColor.b * 1.2f),
                        originalColor.a
                    );
                    _spriteRenderer.color = brighterColor;
                }
            }
            else
            {
                // Reset to normal state
                StopAllCoroutines();
                transform.localScale = _isSelected ? _originalScale * 1.2f : _originalScale;
                
                // Reset any color changes
                if (_spriteRenderer != null)
                {
                    // This is simplified - in a real game you'd want to store the original color
                    SetItemType(_itemType);
                }
            }
        }
        
        private IEnumerator PulseEffect()
        {
            float duration = 0.5f;
            float minScale = 0.9f;
            float maxScale = 1.1f;
            
            Vector3 baseScale = _isSelected ? _originalScale * 1.2f : _originalScale;
            Vector3 minScaleVec = baseScale * minScale;
            Vector3 maxScaleVec = baseScale * maxScale;
            
            while (true) // Loop until stopped externally
            {
                // Pulse up
                float elapsedTime = 0f;
                while (elapsedTime < duration / 2)
                {
                    transform.localScale = Vector3.Lerp(baseScale, maxScaleVec, elapsedTime / (duration / 2));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                // Pulse down
                elapsedTime = 0f;
                while (elapsedTime < duration / 2)
                {
                    transform.localScale = Vector3.Lerp(maxScaleVec, minScaleVec, elapsedTime / (duration / 2));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                // Pulse back to middle
                elapsedTime = 0f;
                while (elapsedTime < duration / 2)
                {
                    transform.localScale = Vector3.Lerp(minScaleVec, baseScale, elapsedTime / (duration / 2));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                yield return new WaitForSeconds(0.2f); // Brief pause between pulses
            }
        }
        
        private IEnumerator ScaleEffect()
        {
            float duration = 0.5f;
            float elapsedTime = 0f;
            
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.zero; // Scale to nothing
            
            while (elapsedTime < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                
                // Also fade out
                if (_spriteRenderer != null)
                {
                    Color color = _spriteRenderer.color;
                    color.a = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                    _spriteRenderer.color = color;
                }
                
                yield return null;
            }
            
            // Ensure we end at zero scale
            transform.localScale = endScale;
        }
        
        public void PositionWithOffset(Vector3 basePosition, bool hasItemInFront)
        {
            if (_isBackLayer)
            {
                Vector3 position = basePosition;
                
                // If there's an item in front, offset the position very slightly
                // so that a hint of the back item is visible from behind the front item
                if (hasItemInFront)
                {
                    position += new Vector3(0.05f, 0.05f, 0f);
                }
                
                // Maintain z-position for layer depth
                position.z = 0.2f;
                transform.position = position;
            }
            else
            {
                // Front items are positioned normally
                basePosition.z = -0.1f;
                transform.position = basePosition;
            }
        }
    }
} 