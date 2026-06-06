# Repository Guidelines

## Project Structure & Module Organization
This is a Unity project. Core gameplay code lives under `Assets/Combat/Scripts/`, shared UI and visual code under `Assets/Core/Scripts/`, and combat data under `Assets/Core/Data/Scriptable Objects/`. Scenes are under `Assets/Core/Scenes/`. Debug and test-only prefabs live in `Assets/Combat/Prefabs/Debug/`, while real creature prefabs live in `Assets/Combat/Prefabs/Creatures/`. Documentation for combat systems lives in `Docs/`.

## Build, Test, and Development Commands
- `dotnet build Assembly-CSharp.csproj`: compile the Unity C# assembly from the repo root.
- `git diff --check`: catch whitespace and line-ending issues before handing off changes.
- Use Unity Editor for play mode validation in `TestMapScene`.

## Coding Style & Naming Conventions
Use the existing C# style in the repo: 4-space indentation, PascalCase for types and public members, camelCase for locals and private fields, and `_camelCase` for serialized private fields. Keep changes narrowly scoped and match nearby code rather than introducing new patterns. Asset names should stay explicit and role-based, for example `Human_DPS`, `Support_AreaSummonMinion_Debug`, or `Effect_Summon_MinorMinion_Debug`.

## Testing Guidelines
There is no separate automated test suite. Validation is mostly through Unity play mode, debug tools, and targeted builds. Prefer `TestMapScene` for combat and skill checks, and use `CreatureCombatDebugTool` / `SkillDebugVfxPresenter` to verify target selection, VFX, and room cleanup. When changing combat logic, confirm the scene still builds and the debug flow works end to end.

## Commit & Pull Request Guidelines
Git history uses short imperative commits, such as `Remove legacy combat prefabs and update assets` or `Add Shield mechanic and visuals`. Keep commits focused and descriptive. Pull requests should explain what changed, why it changed, and how it was validated. Include screenshots or short notes only when a visual or scene workflow changed.

## Agent-Specific Instructions
Do not touch real gameplay data when a debug asset is enough. Avoid architectural rewrites unless the current system blocks the requested change. Prefer local fixes in the owning system over adding new managers or parallel subsystems.
