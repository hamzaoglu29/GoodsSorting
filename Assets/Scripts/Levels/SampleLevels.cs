using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace GoodsSorting.Levels
{
    public class SampleLevels
    {
        [MenuItem("Goods Sorting/Create Sample Levels")]
        public static void CreateSampleLevels()
        {
            CreateLevel1();
            CreateLevel2();
            CreateLevel3();
            CreateLevel4();
            CreateLevel5();
            
            AssetDatabase.SaveAssets();
            Debug.Log("Sample levels created successfully!");
        }
        
        private static void CreateLevel1()
        {
            // Basic tutorial level
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelNumber = 1;
            level.LevelName = "Sorting Basics";
            level.GridWidth = 8;
            level.GridHeight = 8;
            level.MovesLimit = 15;
            level.TimeLimit = 0; // No time limit for tutorial
            
            // Simple goals - collect red and yellow items
            level.SortingGoals = new List<SortingGoal>
            {
                new SortingGoal { ItemType = 0, TargetCount = 5 }, // Collect 5 red items (apples)
                new SortingGoal { ItemType = 1, TargetCount = 5 }  // Collect 5 yellow items (bananas)
            };
            
            // No special powers in tutorial
            level.EnableRandomMatchPower = false;
            level.EnableShufflePower = false;
            
            // Save the level asset
            AssetDatabase.CreateAsset(level, "Assets/Resources/Levels/Level_1.asset");
        }
        
        private static void CreateLevel2()
        {
            // Slightly more challenging level
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelNumber = 2;
            level.LevelName = "Multiple Goals";
            level.GridWidth = 8;
            level.GridHeight = 8;
            level.MovesLimit = 18;
            level.TimeLimit = 0;
            
            // More goals with more items to collect
            level.SortingGoals = new List<SortingGoal>
            {
                new SortingGoal { ItemType = 0, TargetCount = 8 },  // Collect 8 red items
                new SortingGoal { ItemType = 1, TargetCount = 8 },  // Collect 8 yellow items
                new SortingGoal { ItemType = 2, TargetCount = 5 }   // Collect 5 green items
            };
            
            // Introduce random match power
            level.EnableRandomMatchPower = true;
            level.RandomMatchUses = 1;
            level.EnableShufflePower = false;
            
            // Save the level asset
            AssetDatabase.CreateAsset(level, "Assets/Resources/Levels/Level_2.asset");
        }
        
        private static void CreateLevel3()
        {
            // Introduce shuffle power and time pressure
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelNumber = 3;
            level.LevelName = "Time Challenge";
            level.GridWidth = 8;
            level.GridHeight = 8;
            level.MovesLimit = 0; // No move limit
            level.TimeLimit = 120; // 2 minute time limit
            
            // Multiple goals
            level.SortingGoals = new List<SortingGoal>
            {
                new SortingGoal { ItemType = 0, TargetCount = 10 }, // Red
                new SortingGoal { ItemType = 2, TargetCount = 10 }, // Green
                new SortingGoal { ItemType = 4, TargetCount = 5 }   // Purple
            };
            
            // Enable both powers
            level.EnableRandomMatchPower = true;
            level.RandomMatchUses = 1;
            level.EnableShufflePower = true;
            level.ShuffleUses = 1;
            
            // Save the level asset
            AssetDatabase.CreateAsset(level, "Assets/Resources/Levels/Level_3.asset");
        }
        
        private static void CreateLevel4()
        {
            // Introduce obstacles
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelNumber = 4;
            level.LevelName = "Obstacles";
            level.GridWidth = 8;
            level.GridHeight = 8;
            level.MovesLimit = 25;
            level.TimeLimit = 0;
            
            // Add goals with higher targets
            level.SortingGoals = new List<SortingGoal>
            {
                new SortingGoal { ItemType = 1, TargetCount = 12 }, // Yellow
                new SortingGoal { ItemType = 3, TargetCount = 12 }, // Orange
                new SortingGoal { ItemType = 5, TargetCount = 8 }   // Blue
            };
            
            // Enable special powers
            level.EnableRandomMatchPower = true;
            level.RandomMatchUses = 2;
            level.EnableShufflePower = true;
            level.ShuffleUses = 1;
            
            // Add some blocked cells as obstacles
            level.BlockedCells = new List<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(2, 5),
                new Vector2Int(5, 2),
                new Vector2Int(5, 5)
            };
            
            // Save the level asset
            AssetDatabase.CreateAsset(level, "Assets/Resources/Levels/Level_4.asset");
        }
        
        private static void CreateLevel5()
        {
            // Most challenging level with all features
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.LevelNumber = 5;
            level.LevelName = "Full Challenge";
            level.GridWidth = 8;
            level.GridHeight = 8;
            level.MovesLimit = 20;
            level.TimeLimit = 180; // 3 minute time limit
            
            // Add more goals with higher targets
            level.SortingGoals = new List<SortingGoal>
            {
                new SortingGoal { ItemType = 0, TargetCount = 15 }, // Red
                new SortingGoal { ItemType = 1, TargetCount = 15 }, // Yellow
                new SortingGoal { ItemType = 2, TargetCount = 10 }, // Green
                new SortingGoal { ItemType = 3, TargetCount = 10 }  // Orange
            };
            
            // Enable all special powers with more uses
            level.EnableRandomMatchPower = true;
            level.RandomMatchUses = 3;
            level.EnableShufflePower = true;
            level.ShuffleUses = 2;
            
            // Add more complex pattern of blocked cells
            level.BlockedCells = new List<Vector2Int>
            {
                new Vector2Int(1, 1),
                new Vector2Int(1, 6),
                new Vector2Int(6, 1),
                new Vector2Int(6, 6),
                new Vector2Int(3, 3),
                new Vector2Int(3, 4),
                new Vector2Int(4, 3),
                new Vector2Int(4, 4)
            };
            
            // Save the level asset
            AssetDatabase.CreateAsset(level, "Assets/Resources/Levels/Level_5.asset");
        }
    }
}
#endif 