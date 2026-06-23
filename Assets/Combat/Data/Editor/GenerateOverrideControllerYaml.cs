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

    /// <summary>
    /// Maps race → role → component aseprite GUIDs.
    /// The "Body" entry is used as the m_OriginalClip reference for all components.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> AsepriteGuids = new()
    {
        ["Human"] = new()
        {
            ["DPS"] = new()
            {
                ["Body"]  = "999bdcb70ca3efc4d99b57db10aad942",
                ["Cloth"] = "66ffc6daa6dc44440b3329364f3b1329",
                ["Weapon"] = "ac1fc5bca67601c4281b7172054464a5",
            },
            ["Support"] = new()
            {
                ["Body"]  = "e7ebf622d36d3954f9c930a175367ffc",
                ["Cloth"] = "18fe684ce8775054ea7b2af18bbda369",
                ["Weapon"] = "02c8dc2d308e5b247b3d5086bd688feb",
            },
            ["Tank"] = new()
            {
                ["Body"]  = "901afffb30e6e5144955e2066d2be52f",
                ["Cloth"] = "a7c67e4a37bec844fb63389850455017",
                ["Weapon"] = "bbd917feecb1906418a06439e94b673b",
            },
        },
        ["Orc"] = new()
        {
            ["DPS"] = new()
            {
                ["Body"]  = "3f58b3b0c5d171547b983562c8e969ba",
                ["Cloth"] = "7a715a215bafa6f43b57412ac669c1aa",
                ["Weapon"] = "9fd23957cd2724b43af8052d4288c783",
            },
            ["Support"] = new()
            {
                ["Body"]  = "27f2aadd51af0fb49a4bec4a328e4cfd",
                ["Cloth"] = "8ef482700ec964f469cab7fc7cb049c7",
                ["Weapon"] = "42f497a017ad5694ca69709a768b2f2a",
            },
            ["Tank"] = new()
            {
                ["Body"]  = "5a48ae713d5e72948babffa45ef82bb9",
                ["Cloth"] = "31b6890425fa08d4c968c9fc6e97cbb3",
                ["Weapon"] = "be926fb3f044192478ddf92a3983a126",
            },
        },
    };

    [MenuItem("Tools/Combat/Generate Override Controller YAML Files")]
    public static void GenerateAll()
    {
        int generated = 0;

        string assetsPath = Application.dataPath;
        string outputBase = Path.Combine(assetsPath, OutputRoot);

        foreach (var raceEntry in AsepriteGuids)
        {
            string race = raceEntry.Key;

            foreach (var roleEntry in raceEntry.Value)
            {
                string role = roleEntry.Key;

                // The Body GUID is the source of m_OriginalClip for all components
                string bodyGuid = roleEntry.Value["Body"];

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