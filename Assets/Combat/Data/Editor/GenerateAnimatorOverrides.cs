using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor script to generate AnimatorOverrideController assets for each
/// Race (Human, Orc) x Role (DPS, Support, Tank) x Component (Body, Clothes, Weapon, Accessories) combination.
/// </summary>
public static class GenerateAnimatorOverrides
{
    private static readonly string[] Races = { "Human", "Orc" };
    private static readonly string[] Roles = { "DPS", "Support", "Tank" };
    private static readonly string[] Components = { "Body", "Clothes", "Weapon", "Accessories" };

    private const string BaseControllerPath = "Assets/Combat/Animation/Creatures/HumanoidCreatureAnimator.controller";
    private const string OverrideRootFolder = "Assets/Combat/Animation/OverrideControllers";

    [MenuItem("Tools/Combat/Generate Animator Override Controllers")]
    public static void GenerateAll()
    {
        RuntimeAnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);

        if (baseController == null)
        {
            Debug.LogError($"Base controller not found at: {BaseControllerPath}");
            return;
        }

        int totalGenerated = 0;

        foreach (string race in Races)
        {
            foreach (string role in Roles)
            {
                foreach (string component in Components)
                {
                    string folderPath = Path.Combine(
                        OverrideRootFolder,
                        $"{race}_{role}",
                        component);

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = $"{race}_{role}_{component}.overrideController";
                    string assetPath = Path.Combine(folderPath, fileName);

                    // Check if it already exists
                    if (File.Exists(assetPath))
                    {
                        Debug.Log($"Skipping existing: {assetPath}");
                        continue;
                    }

                    AnimatorOverrideController overrideController =
                        new AnimatorOverrideController(baseController);
                    overrideController.name = $"{race}_{role}_{component}";

                    AssetDatabase.CreateAsset(overrideController, assetPath);
                    totalGenerated++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (totalGenerated > 0)
            Debug.Log($"Successfully generated {totalGenerated} AnimatorOverrideController assets.");
        else
            Debug.Log("No new assets were generated (all already exist).");
    }

    [MenuItem("Tools/Combat/Generate Animator Override Controllers", true)]
    private static bool ValidateGenerate()
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath) != null;
    }
}