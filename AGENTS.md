# Repository Guidelines

## Project Structure & Module Organization
`Project-Revenant` is a Unity 6 project (`6000.0.30f1`). Core gameplay code lives in `Assets/Scripts`, organized by feature areas such as `Combat`, `Grid`, `UI`, `Fusion`, `Managers`, and `Inventory`. Scenes are in `Assets/Scenes` (`Dungeon`, `SafeZone`, `DungeonGenerationTest`, etc.). Reusable prefabs, sprites, animations, shaders, and ScriptableObjects live under `Assets/Prefabs`, `Assets/Graphics`, `Assets/Animation`, `Assets/Shaders`, and `Assets/Scriptable Objects`.

Keep code, prefabs, and data close to the system they belong to. When moving or renaming Unity assets, commit the paired `.meta` files.

## Build, Test, and Development Commands
Use the project root as the working directory.

```powershell
dotnet build Assembly-CSharp.csproj
```
Builds runtime C# scripts to catch compiler errors quickly outside the Unity Editor.

```powershell
dotnet build Assembly-CSharp-Editor.csproj
```
Builds editor-only code if you add tooling or inspectors.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -quit
```
Runs Unity Test Framework Edit Mode tests from the command line. The package is installed, but the repo currently has almost no formal test coverage.

## Coding Style & Naming Conventions
Use 4 spaces for indentation and standard C# brace style. Follow the existing naming pattern: `PascalCase` for types, methods, and public properties; `_camelCase` for private serialized fields; descriptive singular class names such as `Unit`, `FusionService`, and `ShopUIManager`.

Prefer small MonoBehaviours with focused responsibilities. Keep Unity-specific references `[SerializeField] private` unless broader access is required.

Do not rely on `AddComponent` at runtime to complete the `Necromancer` setup. Scripts required by the player character must be present on the prefab and wired in authoring time.

## Testing Guidelines
Add Edit Mode or Play Mode tests when changing combat rules, grid logic, status effects, or procedural generation. Name test files after the unit under test, for example `GridPathfinderTests.cs`. If you add temporary scene-side probes like `MovementRangeTest.cs`, remove or convert them before merging.

## Commit & Pull Request Guidelines
Recent commits use short imperative subjects such as `Add shop system: service, data, and controller` and `Refactor feedback UI, prefabs and life UI code`. Keep commit titles concise, capitalized, and action-first.

PRs should summarize gameplay impact, list touched scenes/prefabs/ScriptableObjects, and include screenshots or short clips for UI or visual changes. Note how you validated the change (`dotnet build`, Unity playtest, Edit Mode tests) and link the related task or issue when available.
