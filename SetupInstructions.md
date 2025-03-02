# Goods Sorting Match 3 Game - Setup Instructions

This document contains comprehensive step-by-step instructions for setting up, running, and developing the Goods Sorting Match 3 Game.

## Requirements

- Unity 2022.3 LTS or newer (recommended)
- Basic knowledge of Unity Editor
- Git for version control (optional but recommended)
- Android SDK for mobile testing (if targeting Android)
- iOS development tools (if targeting iOS)

## Project Setup

### Method 1: Setup From Scratch

1. **Create a new Unity project:**
   - Open Unity Hub
   - Click "New Project"
   - Select "2D" template
   - Name the project "GoodsSortingMatch3"
   - Click "Create Project"

2. **Import the code files:**
   - Copy the contents of the provided `Assets` folder into your project's `Assets` folder
   - Allow Unity to compile the scripts

3. **Create required folders and assets:**
   
   The project requires certain folders and assets to be created. You can do this manually or use the provided editor tools:
   
   - In Unity Editor, go to the top menu and select:
     - `Goods Sorting > Create Item Prefabs` (creates basic item prefabs)
     - `Goods Sorting > Create Sample Levels` (creates level data assets)

4. **Set up the main scene:**
   
   Create a new scene called "MainScene" with the following hierarchy:
   
   ```
   - GameManager
     - GridManager
     - LevelManager
     - SortingManager
     - UIManager
     - PowerManager
   - Grid
     - GridContainer
     - GridVisualizer
   - UI
     - Canvas
       - MainMenuPanel
       - LevelSelectPanel
       - GameplayPanel
       - PausePanel
       - LevelCompletePanel
       - LevelFailedPanel
     - EventSystem
   - Camera
   ```

5. **Assign script components:**
   
   - Attach the respective script components to their GameObjects
   - Connect references between components (e.g., GridManager reference in SortingManager)
   - Assign prefabs and UI references in the Inspector

### Method 2: Clone from Git Repository

1. **Clone the repository:**
   ```
   git clone https://github.com/yourusername/GoodsSortingMatch3.git
   ```

2. **Open the project in Unity:**
   - Launch Unity Hub
   - Click "Add" and browse to the cloned repository folder
   - Select the project and open it

3. **Verify project structure:**
   - Check that all scripts, prefabs, and scenes are properly imported
   - If any errors appear, ensure all dependencies are met

## Git Version Control Setup

1. **Initialize Git (if not cloning existing repository):**
   ```
   git init
   ```

2. **Add the included .gitignore file:**
   - The project includes a `.gitignore` file specifically tailored for Unity projects
   - This ensures that unnecessary files (like Library, Temp, and build files) are not tracked

3. **Make initial commit:**
   ```
   git add .
   git commit -m "Initial commit"
   ```

4. **Add remote repository (optional):**
   ```
   git remote add origin https://github.com/yourusername/GoodsSortingMatch3.git
   git push -u origin master
   ```

## Running the Game

1. **Test in Editor:**
   - Open the `Assets/Scenes/MainScene.unity` file
   - Press Play in the Unity Editor to test the game
   - The game should start at the main menu
   - Navigate through the UI to play levels

2. **Build for Desktop:**
   - Go to File > Build Settings
   - Add the MainScene to the build
   - Select Windows/Mac/Linux platform
   - Click "Build" to create an executable

3. **Build for Mobile:**
   - Go to File > Build Settings
   - Add the MainScene to the build
   - Select Android or iOS platform
   - Click "Switch Platform"
   - Configure Player Settings:
     - Set proper package name (e.g., com.yourusername.goodssorting)
     - Set orientation to Portrait
     - Under "Other Settings", set minimum API level (Android 6.0/API 23 recommended)
     - For iOS, configure signing settings
   - Click "Build" or "Build and Run" to create the executable

## Mobile Testing

### Android Testing:

1. **Set up Android SDK:**
   - Install Android SDK via Unity Hub
   - Ensure you have the right platform tools and build tools installed

2. **Enable USB Debugging on your device:**
   - Go to Settings > About Phone
   - Tap "Build Number" 7 times to enable Developer Options
   - Go to Settings > Developer Options
   - Enable "USB Debugging"

3. **Connect and test:**
   - Connect your Android device via USB
   - In Unity, select "Build and Run" in Build Settings
   - Test performance, touch input, and UI scaling on the device

4. **Use Profiler:**
   - With device connected, open Window > Analysis > Profiler
   - Connect to Android Player
   - Monitor performance metrics during gameplay

### iOS Testing:

1. **Xcode setup:**
   - Ensure you have the latest Xcode installed
   - Configure your Apple Developer account in Xcode

2. **Build and deploy:**
   - Build to Xcode project from Unity
   - Open the generated project in Xcode
   - Set up signing and capabilities
   - Deploy to a connected iOS device or simulator

## Performance Optimization

For the best performance, especially on mobile devices:

1. **Disable excessive logging:**
   - In production builds, minimize or eliminate Debug.Log calls
   - The codebase has been optimized by commenting out non-essential logs

2. **Graphics settings:**
   - In Player Settings, adjust quality settings for mobile
   - Consider using texture compression appropriate for your target devices

3. **Memory management:**
   - The game uses object pooling for grid items
   - Monitor memory usage with the Profiler to identify leaks

## Troubleshooting

- **Missing references:** Make sure all script references are properly assigned in the Inspector
- **Compilation errors:** Check for any namespace issues or missing dependencies
- **UI issues:** Ensure the Canvas Scaler is set to "Scale With Screen Size" for proper mobile scaling
- **Git problems:** If having issues with large files, ensure the .gitignore file is properly set up

## Development Notes

- The grid size is configurable in the GridManager (default 8x8)
- The dual-layer system (front/back) creates strategic depth
- Sorting sections add complexity to the gameplay
- Level data is stored in ScriptableObjects for easy editing
- Add proper sprites for items instead of colored rectangles for production use

## Project Documentation

Refer to these additional documentation files:

- **README.md** - Overview of the game and high-level instructions
- **DevelopmentRoadmap.md** - Details on development approach and architecture
- **OptimizationReport.txt** - Performance optimizations and testing guidelines

## Contact

If you encounter any issues or have questions about the implementation, please contact the developer. 