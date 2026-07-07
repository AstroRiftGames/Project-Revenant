using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Generates AnimatorOverrideController assets using the Unity API
/// so that animation clip references are properly resolved.
/// 
/// Usage: Tools > Combat > Generate All Override Controllers
/// </summary>
public static class AnimationOverrideGenerator
{
    private const string BaseControllerPath = "Assets/Combat/Animation/Creatures/New/UnitAnimator.controller";
    private const string OutputRoot = "Assets/Combat/Animation/Creatures/New";

    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> DiscoverAsepritePaths()
    {
        var discovered = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(System.StringComparer.OrdinalIgnoreCase);
        string rootDir = "Assets/Combat/Graphics/Characters";

        if (!Directory.Exists(rootDir))
        {
            Debug.LogError($"Directory not found: {rootDir}");
            return discovered;
        }

        string[] files = Directory.GetFiles(rootDir, "*.aseprite", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string path = file.Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(path);

            string race = null;
            if (path.IndexOf("/Human/", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("/Humans/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                race = "Human";
            }
            else if (path.IndexOf("/Orc/", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     path.IndexOf("/Orcs/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                race = "Orc";
            }

            if (race == null) continue;

            string role = null;
            if (fileName.IndexOf("_DPS_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                role = "DPS";
            else if (fileName.IndexOf("_Support_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                role = "Support";
            else if (fileName.IndexOf("_Tank_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                role = "Tank";

            if (role == null) continue;

            string component = "Weapon";
            if (fileName.IndexOf("_Body_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                component = "Body";
            else if (fileName.IndexOf("_Clothes_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     fileName.IndexOf("_Cloth_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                component = "Cloth";
            else if (fileName.IndexOf("_Accessories_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     fileName.IndexOf("_Accessory_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                component = "Accessories";

            if (!discovered.ContainsKey(race))
                discovered[race] = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);
            if (!discovered[race].ContainsKey(role))
                discovered[race][role] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            discovered[race][role][component] = path;
        }

        return discovered;
    }

    private static string GetDirection(AnimationClip clip)
    {
        if (clip == null) return string.Empty;

        string clipName = clip.name;
        string assetPath = AssetDatabase.GetAssetPath(clip);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (clipName.StartsWith(fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                clipName = clipName.Substring(fileName.Length);
            }
        }

        string lower = clipName.ToLowerInvariant();
        string[] parts = lower.Split('_');
        string lastPart = parts.Length > 0 ? parts[parts.Length - 1] : lower;

        bool IsDir(string dir) => lastPart == dir || lower.EndsWith("_" + dir) || lower.EndsWith(dir);

        if (IsDir("northeast") || IsDir("upright")) return "northeast";
        if (IsDir("northwest") || IsDir("upleft")) return "northwest";
        if (IsDir("southeast") || IsDir("downright")) return "southeast";
        if (IsDir("southwest") || IsDir("downleft")) return "southwest";
        if (IsDir("north") || IsDir("up")) return "north";
        if (IsDir("south") || IsDir("down")) return "south";
        if (IsDir("east") || IsDir("right")) return "east";
        if (IsDir("west") || IsDir("left")) return "west";

        return string.Empty;
    }

    [MenuItem("Tools/Combat/Generate All Override Controllers")]
    public static void GenerateAll()
    {
        // Load the base controller
        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);

        if (baseController == null)
        {
            Debug.LogError($"Base controller not found at: {BaseControllerPath}");
            return;
        }

        var AsepritePaths = DiscoverAsepritePaths();
        int generated = 0;
        int errors = 0;

        foreach (var raceEntry in AsepritePaths)
        {
            string race = raceEntry.Key;

            foreach (var roleEntry in raceEntry.Value)
            {
                string role = roleEntry.Key;

                // Load the Body aseprite to get the original clips
                if (!roleEntry.Value.TryGetValue("Body", out string bodyAsepritePath))
                {
                    Debug.LogError($"No Body component found for race {race}, role {role}. Skip generating components.");
                    errors++;
                    continue;
                }
                AnimationClip[] bodyClips = LoadAnimationClips(bodyAsepritePath);

                if (bodyClips == null || bodyClips.Length == 0)
                {
                    Debug.LogError($"No clips found in body aseprite: {bodyAsepritePath}");
                    errors++;
                    continue;
                }

                foreach (var compEntry in roleEntry.Value)
                {
                    string component = compEntry.Key;
                    string asepritePath = compEntry.Value;

                    // Load the override clips from the component's aseprite
                    AnimationClip[] overrideClips = LoadAnimationClips(asepritePath);

                    if (overrideClips == null || overrideClips.Length == 0)
                    {
                        Debug.LogError($"No clips found in aseprite: {asepritePath}");
                        errors++;
                        continue;
                    }

                    // Create the override controller
                    AnimatorOverrideController overrideController =
                        new AnimatorOverrideController(baseController);

                    string controllerName = $"{race}-{role}-{component}";
                    overrideController.name = controllerName;

                    // Get the current override list (pairs of original → override)
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideController.GetOverrides(overrides);

                    // Map each original clip to the corresponding override clip by matching their direction suffix.
                    for (int i = 0; i < overrides.Count; i++)
                    {
                        AnimationClip originalClip = overrides[i].Key;
                        string direction = GetDirection(originalClip);
                        AnimationClip matchingOverride = System.Array.Find(overrideClips, c => GetDirection(c) == direction);

                        if (matchingOverride != null)
                        {
                            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, matchingOverride);
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find matching override clip for direction '{direction}' (original clip: {originalClip.name}) in component {component} of {race}-{role}");
                        }
                    }

                    overrideController.ApplyOverrides(overrides);

                    // Save as asset
                    string folderPath = Path.Combine(OutputRoot, race, role);
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        string parent = Path.Combine(OutputRoot, race);
                        if (!AssetDatabase.IsValidFolder(parent))
                        {
                            string grandParent = OutputRoot;
                            if (!AssetDatabase.IsValidFolder(grandParent))
                                AssetDatabase.CreateFolder("Assets/Combat/Animation/Creatures", "New");
                            AssetDatabase.CreateFolder(grandParent, race);
                        }
                        AssetDatabase.CreateFolder(parent, role);
                    }

                    string assetPath = Path.Combine(folderPath, $"{controllerName}.overrideController");

                    // Delete existing asset if present
                    AnimatorOverrideController existing =
                        AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(assetPath);
                    if (existing != null)
                        AssetDatabase.DeleteAsset(assetPath);

                    AssetDatabase.CreateAsset(overrideController, assetPath);
                    generated++;

                    Debug.Log($"Created: {assetPath} ({overrideClips.Length} direction clips)");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Done!</color> Generated {generated} override controllers. {errors} errors.");
    }

    /// <summary>
    /// Loads all AnimationClip sub-assets from an .aseprite file.
    /// The Aseprite importer generates one clip per layer.
    /// </summary>
    private static AnimationClip[] LoadAnimationClips(string asepritePath)
    {
        Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(asepritePath);
        List<AnimationClip> clips = new();

        foreach (Object subAsset in subAssets)
        {
            if (subAsset is AnimationClip clip)
            {
                clips.Add(clip);
            }
        }

        return clips.ToArray();
    }

    [MenuItem("Tools/Combat/Generate All Override Controllers", true)]
    private static bool ValidateGenerate()
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath) != null;
    }
}