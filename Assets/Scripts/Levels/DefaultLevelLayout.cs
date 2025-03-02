using UnityEngine;

namespace GoodsSorting.Levels
{
    public static class DefaultLevelLayout
    {
        // Creates a default level layout where all items are guaranteed to be matchable
        public static int[] CreateMatchableLayout(int width, int height, int numSections)
        {
            int[] layout = new int[width * height];
            
            // Initialize all cells to empty
            for (int i = 0; i < layout.Length; i++)
            {
                layout[i] = -1;
            }
            
            // Calculate columns per section
            int columnsPerSection = width / numSections;
            
            // Create a pattern of item types that ensures every item can be matched
            for (int section = 0; section < numSections; section++)
            {
                // For each section, create rows of the same item
                for (int y = 0; y < height; y++)
                {
                    // Choose a distinct item type for each row in this section
                    int itemType = (section + y) % 6; // 6 item types (0-5)
                    
                    // Place 3 matching items in each row of this section
                    for (int i = 0; i < 3; i++)
                    {
                        int x = section * columnsPerSection + i;
                        
                        // Skip one cell in the middle of each section to make a more interesting layout
                        if (y == section % height && i == 1)
                        {
                            continue; // Leave as empty
                        }
                        
                        int index = y * width + x;
                        if (index < layout.Length)
                        {
                            layout[index] = itemType;
                        }
                    }
                }
            }
            
            // Validate that all items are in multiples of 3
            int[] itemCounts = new int[6];
            for (int i = 0; i < layout.Length; i++)
            {
                if (layout[i] >= 0 && layout[i] < 6)
                {
                    itemCounts[layout[i]]++;
                }
            }
            
            // Ensure each item type has a count divisible by 3
            for (int type = 0; type < itemCounts.Length; type++)
            {
                int remainder = itemCounts[type] % 3;
                if (remainder != 0)
                {
                    // Find empty spaces to add the missing items
                    int needed = 3 - remainder;
                    for (int i = 0; i < layout.Length && needed > 0; i++)
                    {
                        if (layout[i] == -1)
                        {
                            layout[i] = type;
                            needed--;
                        }
                    }
                }
            }
            
            // Log the final layout for debugging
            string layoutStr = "Created matchable layout:\n";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    layoutStr += layout[y * width + x] + " ";
                }
                layoutStr += "\n";
            }
            Debug.Log(layoutStr);
            
            return layout;
        }
    }
} 