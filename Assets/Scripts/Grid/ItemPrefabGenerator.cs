using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace GoodsSorting.Grid
{
    public class ItemPrefabGenerator
    {
        [MenuItem("Goods Sorting/Create Item Prefabs")]
        public static void CreateItemPrefabs()
        {
            // Create the Resources/Prefabs directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
                
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
                
            // Create different item prefabs
            CreateItemPrefab("RedItem", Color.red); // Apple
            CreateItemPrefab("YellowItem", Color.yellow); // Banana
            CreateItemPrefab("GreenItem", Color.green); // Lime
            CreateItemPrefab("OrangeItem", new Color(1f, 0.5f, 0f)); // Orange
            CreateItemPrefab("PurpleItem", new Color(0.5f, 0f, 0.5f)); // Grape
            CreateItemPrefab("BlueItem", new Color(0f, 0.5f, 1f)); // Blueberry
            
            AssetDatabase.SaveAssets();
            Debug.Log("Item prefabs created successfully!");
        }
        
        private static void CreateItemPrefab(string name, Color color)
        {
            // Create a game object with a sprite renderer
            GameObject itemObj = new GameObject(name);
            SpriteRenderer spriteRenderer = itemObj.AddComponent<SpriteRenderer>();
            
            // Set up the sprite renderer with a default sprite and the specified color
            spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            spriteRenderer.color = color;
            
            // Add the GridItem component
            GridItem gridItem = itemObj.AddComponent<GridItem>();
            
            // Add a box collider for interaction
            BoxCollider2D boxCollider = itemObj.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 0.8f); // Slightly smaller than the sprite
            
            // Adjust the transform
            itemObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            
            // Create the prefab
            string prefabPath = $"Assets/Resources/Prefabs/{name}.prefab";
            Object prefab = PrefabUtility.SaveAsPrefabAsset(itemObj, prefabPath);
            
            // Clean up the temporary object
            Object.DestroyImmediate(itemObj);
        }
    }
}
#endif 