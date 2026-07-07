using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Generates AnimatorOverrideController .overrideController YAML files
/// with correct per-direction animation clip assignments from .aseprite files.
/// 
/// Each .aseprite generates 8 AnimationClip sub-assets (one per direction)
/// with consistent FileIDs across all aseprite files.
/// 
/// Usage: Tools > Combat > Generate Override Controller YAML Files
/// </summary>
public static class GenerateOverrideControllerYaml
{
    // Base controller GUID (UnitAnimator.controller in Creatures/New/)
    private const string BaseControllerGuid = "608917a3c53651c47a8e58d577c7f89a";
    private const string BaseControllerFileId = "9100000";

    // These FileIDs are consistent across all .aseprite files that generate
    // 8-direction idle animations.
    // Order specified by the user:
    // 1) Idle_East - Right
    // 2) Idle_North - Up
    // 3) Idle_NorthEast - UpRight
    // 4) Idle_NorthWest - UpLeft
    // 5) Idle_South - Down
    // 6) Idle_SouthEast - DownRight
    // 7) Idle_SouthWest - DownLeft
    // 8) Idle_West - Left
    private static readonly long[] DirectionClipFileIds =
    {
        429540359091499943,   // 1) East  - Right
        8451551337587442842,  // 2) North - Up
        2039607144426384982,  // 3) NorthEast - UpRight
        -5148657230702060658, // 4) NorthWest - UpLeft
        1624593205714598167,  // 5) South - Down
        -3332656750375472118, // 6) SouthEast - DownRight
        -5637541457207421186, // 7) SouthWest - DownLeft
        2242093226116659609,  // 8) West - Left
    };

    // Output folder (relative to Assets/)
    private const string OutputRoot = "Combat/Animation/Creatures/New";

    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> DiscoverAsepriteGuids()
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

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"Could not find GUID for asset at path: {path}");
                continue;
            }

            if (!discovered.ContainsKey(race))
                discovered[race] = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);
            if (!discovered[race].ContainsKey(role))
                discovered[race][role] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            discovered[race][role][component] = guid;
        }

        return discovered;
    }

    [MenuItem("Tools/Combat/Generate Override Controller YAML Files")]
    public static void GenerateAll()
    {
        int generated = 0;

        string assetsPath = Application.dataPath;
        string outputBase = Path.Combine(assetsPath, OutputRoot);

        var discoveredGuids = DiscoverAsepriteGuids();

        foreach (var raceEntry in discoveredGuids)
        {
            string race = raceEntry.Key;

            foreach (var roleEntry in raceEntry.Value)
            {
                string role = roleEntry.Key;

                // The Body GUID is the source of m_OriginalClip for all components
                if (!roleEntry.Value.TryGetValue("Body", out string bodyGuid))
                {
                    Debug.LogError($"No Body component found for race {race}, role {role}. Skip generating components.");
                    continue;
                }

                foreach (var compEntry in roleEntry.Value)
                {
                    string component = compEntry.Key;
                    string asepriteGuid = compEntry.Value;

                    string folderPath = Path.Combine(outputBase, race, role);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = $"{race}-{role}-{component}.overrideController";
                    string filePath = Path.Combine(folderPath, fileName);

                    string yaml = GenerateYaml(race, role, component, bodyGuid, asepriteGuid);
                    File.WriteAllText(filePath, yaml);

                    generated++;
                    Debug.Log($"Generated: {fileName}");
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Done!</color> Generated {generated} files with correct direction order.");
    }

    /// <summary>
    /// Generates the YAML for an AnimatorOverrideController.
    /// m_OriginalClip always references the Body aseprite.
    /// m_OverrideClip references the specific component's aseprite.
    /// </summary>
    private static string GenerateYaml(string race, string role, string component, string originalGuid, string overrideGuid)
    {
        var lines = new List<string>
        {
            "%YAML 1.1",
            "%TAG !u! tag:unity3d.com,2011:",
            "--- !u!221 &22100000",
            "AnimatorOverrideController:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            $"  m_Name: {race}-{role}-{component}",
            $"  m_Controller: {{fileID: {BaseControllerFileId}, guid: {BaseControllerGuid}, type: 2}}",
            "  m_Clips:",
        };

        foreach (long fileId in DirectionClipFileIds)
        {
            lines.Add("  - m_OriginalClip: {fileID: " + fileId + ", guid: " + originalGuid + ", type: 3}");
            lines.Add("    m_OverrideClip: {fileID: " + fileId + ", guid: " + overrideGuid + ", type: 3}");
        }

        return string.Join("\n", lines) + "\n";
    }

    [MenuItem("Tools/Combat/Generate Override Controller YAML Files", true)]
    private static bool ValidateGenerate()
    {
        string assetsPath = Application.dataPath;
        string outputBase = Path.Combine(assetsPath, OutputRoot);
        return Directory.Exists(outputBase);
    }
}