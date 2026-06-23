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

    /// <summary>
    /// Maps race → role → component → aseprite asset path
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> AsepritePaths = new()
    {
        ["Human"] = new()
        {
            ["DPS"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Human/Humans_DPS_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Human/Humans_DPS_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Human/Humans_DPS_Axes_Idle.aseprite",
            },
            ["Support"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Human/Humans_Support_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Human/Humans_Support_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Human/Humans_Support_Bow_Idle.aseprite",
            },
            ["Tank"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Human/Humans_Tank_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Human/Humans_Tank_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Human/Humans_Tank_Spear&Shield_Idle.aseprite",
            },
        },
        ["Orc"] = new()
        {
            ["DPS"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Orcs/Orcs_DPS_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_DPS_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_DPS_Axes_Idle.aseprite",
            },
            ["Support"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Support_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Support_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Support_Bow_Idle.aseprite",
            },
            ["Tank"] = new()
            {
                ["Body"]  = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Tank_Body_Idle.aseprite",
                ["Cloth"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Tank_Clothes_Idle.aseprite",
                ["Weapon"] = "Assets/Combat/Graphics/Characters/Orcs/Orcs_Tank_Spear&Shield_Idle.aseprite",
            },
        },
    };

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

        int generated = 0;
        int errors = 0;

        foreach (var raceEntry in AsepritePaths)
        {
            string race = raceEntry.Key;

            foreach (var roleEntry in raceEntry.Value)
            {
                string role = roleEntry.Key;

                // Load the Body aseprite to get the original clips
                string bodyAsepritePath = roleEntry.Value["Body"];
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

                    // Map each original clip to the corresponding override clip
                    // The order in m_Clips must match the user's specification:
                    // 1) East (Right), 2) North (Up), 3) NorthEast (UpRight),
                    // 4) NorthWest (UpLeft), 5) South (Down), 6) SouthEast (DownRight),
                    // 7) SouthWest (DownLeft), 8) West (Left)
                    for (int i = 0; i < overrides.Count && i < overrideClips.Length; i++)
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                            overrides[i].Key, overrideClips[i]);
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