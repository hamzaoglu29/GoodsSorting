# Goods Sorting Match-3 Game

A unique take on the Match-3 genre that combines traditional matching mechanics with goods sorting and strategic empty space management.

## Game Overview

In Goods Sorting Match-3, players match items by swapping them with empty spaces on the grid rather than adjacent items. The game features a dual-layer grid system, where items in the back layer are promoted to the front layer when front rows are cleared.

Key features:
- Unique empty space swapping mechanic
- Section-based grid for strategic gameplay
- Dual-layer system (front and back)
- Power-ups including random matches and board shuffling
- Time-limited levels for added challenge
- Progressive difficulty through multiple levels

## Installation and Setup

### Prerequisites
- Unity 2022.3 or newer
- Basic knowledge of Unity

### Installation Steps
1. Clone the repository:
   ```
   git clone https://github.com/hamzaoglu29/GoodsSorting.git
   ```
2. Open the project in Unity:
   - Launch Unity Hub
   - Click "Add" and browse to the cloned repository folder
   - Select the project and open it

3. Once opened, navigate to the main scene:
   - Open the `Assets/Scenes/MainScene.unity` file

4. Press the Play button to test the game in the Unity Editor

## Game Controls
- **Click/Touch**: Select an item or empty space
- **Drag**: (Alternative) Drag an item to an empty space to swap

## Mobile Build Instructions
1. Open the Build Settings (File > Build Settings)
2. Select Android or iOS as the target platform
3. Click "Switch Platform" if not already selected
4. Configure Player Settings for your target device
5. Click "Build" or "Build and Run" to create the executable

## Project Structure

### Key Directories
- `/Assets/Scripts/`: Contains all game scripts organized by functionality
  - `/Grid/`: Grid system logic
  - `/Managers/`: Game managers (GameManager, LevelManager, etc.)
  - `/UI/`: User interface components
  - `/Levels/`: Level data and configurations
- `/Assets/Prefabs/`: Reusable game objects
- `/Assets/Scenes/`: Game scenes
- `/Assets/Resources/`: Assets loaded at runtime

### Key Components
- `GridManager`: Manages the game grid and item interactions
- `InputHandler`: Processes player input
- `LevelManager`: Handles level loading and progression
- `SortingManager`: Manages item sorting and objective tracking
- `PowerManager`: Controls power-up functionality
- `UIManager`: Manages UI elements and transitions

## Development

### Customizing Levels
You can create new levels by:
1. Duplicating an existing level data asset in `/Assets/Resources/Levels/`
2. Adjusting parameters like:
   - Grid dimensions
   - Time limit
   - Required items
   - Starting item layout
   - Available power-ups

### Adding New Item Types
To add new item types:
1. Create sprite assets for the new items
2. Add prefabs to the GridManager's item prefabs array
3. Update the item type enumeration
4. Modify the item spawning logic to include the new types

## Performance Considerations
- The game uses object pooling for grid items to improve performance
- Visual effects are optimized for mobile devices
- Event-driven architecture reduces unnecessary updates

## Troubleshooting
- If the grid appears incomplete, check the grid dimensions in the inspector
- For performance issues, reduce the number of visual effects
- If matching doesn't work correctly, verify the section configuration

## License
This project is licensed under the MIT License - see the LICENSE file for details. 
