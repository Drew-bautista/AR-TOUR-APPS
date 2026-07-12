Home UI Prefab — automated setup

What this script does
- Tools -> Generate Home UI Assets: create placeholder sprites (gradient, radial glow, icons) in Assets/Art/UI
- Tools -> Wire Home UI Assets: assigns placeholder sprites to HomeScreenUIController in the current scene
- Tools -> Create Full Home UI Prefab: builds the UI, creates a simple Animator (Canvas fade-in), and creates Assets/Art/UI/HomeUI.prefab

How to run
1. In Unity Editor, wait for compilation.
2. Menu -> Tools -> Generate Home UI Assets (if you haven't already).
3. Add an empty GameObject to the scene and attach HomeScreenUIController, or let the Create Full Home UI Prefab tool create it.
4. Menu -> Tools -> Create Full Home UI Prefab — this will generate the prefab at Assets/Art/UI/HomeUI.prefab

Notes
- The editor tools use Editor APIs and run in the Editor only.
- For smoother animations, install DOTween and define the scripting symbol ENABLE_DOTWEEN in Player Settings -> Scripting Define Symbols; then attach HomeScreenDOTweenController and call PlayEntrance().
- The BuildNow() method is available on HomeScreenUIController (Context menu: right-click component -> Build UI Now) to re-generate the UI in the scene.
