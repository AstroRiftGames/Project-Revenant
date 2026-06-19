using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Systems;
using Interactables.Portals;
using PrefabDungeonGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class PrototypeReadinessValidator
{
    private const string MenuPath = "Tools/Validation/Validate Prototype Readiness";
    private const string RoomsFolder = "Assets/Dungeon/Prefabs/Rooms";
    private const string SafeZoneScenePath = "Assets/Core/Scenes/SafeZone.unity";
    private const string DungeonScenePath = "Assets/Dungeon/Scenes/Dungeon.unity";
    private const string SafeZoneRoomPrefabPath = "Assets/Dungeon/Prefabs/Rooms/SafeZoneRoom.prefab";
    private const string ManagerPartySearch = "[Manager_Party] t:Prefab";
    private const string MarkdownReportPath = "Docs/Validation/PrototypeReadinessReport.md";

    [MenuItem(MenuPath)]
    public static void ValidatePrototypeReadiness()
    {
        PrototypeValidationReport report = RunValidation();
        report.WriteMarkdownReport(MarkdownReportPath);
        report.LogToConsole();

        string dialogMessage =
            $"Errors: {report.ErrorCount}\nWarnings: {report.WarningCount}\nInfo: {report.InfoCount}\n" +
            $"Failed Assets: {report.FailedAssets.Count}\nPassed Assets: {report.PassedAssets.Count}\n" +
            $"Markdown: {MarkdownReportPath}";

        EditorUtility.DisplayDialog("Prototype Readiness Validation", dialogMessage, "OK");
    }

    public static void ValidatePrototypeReadinessBatchMode()
    {
        PrototypeValidationReport report = RunValidation();
        report.WriteMarkdownReport(MarkdownReportPath);
        report.LogToConsole();

        if (Application.isBatchMode && report.ErrorCount > 0)
            throw new InvalidOperationException($"Prototype readiness validation failed with {report.ErrorCount} blocking errors.");
    }

    private static PrototypeValidationReport RunValidation()
    {
        var report = new PrototypeValidationReport();

        ValidateBuildSettings(report);
        ValidateRoomPrefabs(report);
        ValidateScenes(report);
        ValidateManagerPrefab(report);
        ValidateUnitDataAndCreaturePrefabs(report);

        report.FinalizeAssetBuckets();
        return report;
    }

    private static void ValidateBuildSettings(PrototypeValidationReport report)
    {
        var requiredScenePaths = new[]
        {
            SafeZoneScenePath,
            DungeonScenePath
        };

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        var seenPaths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = scenes[i];
            if (scene == null || string.IsNullOrWhiteSpace(scene.path))
                continue;

            if (!seenPaths.TryAdd(scene.path, 1))
            {
                seenPaths[scene.path]++;
                report.Error(
                    "BuildSettings",
                    scene.path,
                    "Scene",
                    $"Scene is duplicated in Build Settings ({seenPaths[scene.path]} entries).",
                    "Keep a single enabled entry for the scene.");
            }

            if (!scene.enabled)
            {
                string suggestion = requiredScenePaths.Contains(scene.path, StringComparer.OrdinalIgnoreCase)
                    ? "Enable the scene because the current SafeZone -> Dungeon flow depends on it."
                    : "Remove the disabled scene or document why it must stay disabled.";

                if (requiredScenePaths.Contains(scene.path, StringComparer.OrdinalIgnoreCase))
                {
                    report.Error(
                        "BuildSettings",
                        scene.path,
                        "Scene",
                        "Required scene is present but disabled in Build Settings.",
                        suggestion);
                }
                else
                {
                    report.Warning(
                        "BuildSettings",
                        scene.path,
                        "Scene",
                        "Scene is present but disabled in Build Settings.",
                        suggestion);
                }
            }
        }

        for (int i = 0; i < requiredScenePaths.Length; i++)
        {
            string requiredScenePath = requiredScenePaths[i];
            bool exists = scenes.Any(scene => scene != null && string.Equals(scene.path, requiredScenePath, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                report.Error(
                    "BuildSettings",
                    requiredScenePath,
                    "Scene",
                    "Required scene is missing from Build Settings.",
                    "Add the scene to Build Settings for the current flow.");
            }
            else
            {
                report.Pass(requiredScenePath);
            }
        }
    }

    private static void ValidateRoomPrefabs(PrototypeValidationReport report)
    {
        string[] roomGuids = AssetDatabase.FindAssets("t:Prefab", new[] { RoomsFolder });
        if (roomGuids.Length == 0)
        {
            report.Error(
                "Rooms",
                RoomsFolder,
                "Folder",
                "No room prefabs were found in the expected folder.",
                "Confirm the room prefab path or update the validator constants.");
            return;
        }

        for (int i = 0; i < roomGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(roomGuids[i]);
            ValidateRoomPrefab(assetPath, report);
        }
    }

    private static void ValidateRoomPrefab(string assetPath, PrototypeValidationReport report)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            ValidateMissingScripts(prefabRoot, assetPath, "Room Prefab", report);

            RoomContext[] roomContexts = prefabRoot.GetComponentsInChildren<RoomContext>(true);
            if (roomContexts.Length == 0)
            {
                report.Error("Room Prefab", assetPath, nameof(RoomContext), "Room prefab is missing RoomContext.", "Add a single RoomContext to the room root.");
                return;
            }

            if (roomContexts.Length > 1)
            {
                report.Error("Room Prefab", assetPath, nameof(RoomContext), $"Room prefab contains {roomContexts.Length} RoomContext components.", "Keep a single RoomContext per room prefab.");
            }

            RoomContext roomContext = roomContexts[0];
            SerializedObject roomContextSerialized = new SerializedObject(roomContext);
            RoomPrefabProfile roomProfile = ResolveRoomProfile(roomContext, roomContextSerialized);
            bool isSpecialNonProceduralRoom = IsSpecialNonProceduralRoomPrefab(assetPath);
            bool isCombatRoom = RoomCombatUtility.IsCombatRoom(roomProfile);

            ValidateDuplicateComponents<RoomGrid>(prefabRoot, assetPath, "Room Prefab", report, dangerous: true);
            ValidateDuplicateComponents<CombatRoomController>(prefabRoot, assetPath, "Room Prefab", report, dangerous: true);
            ValidateDuplicateComponents<RoomCameraBounds>(prefabRoot, assetPath, "Room Prefab", report, dangerous: false);

            RoomGrid roomGrid = ResolveRoomGrid(roomContext, roomContextSerialized);
            if (roomGrid == null)
            {
                report.Error("Room Prefab", assetPath, nameof(RoomGrid), "RoomGrid is missing and cannot be resolved from children.", "Add a RoomGrid child or wire the RoomContext reference.");
            }

            Tilemap walkableTilemap = ResolveTilemap(roomContext, roomContextSerialized, "_walkableTilemap", "FloorTilemap", "WalkableTilemap");
            if (walkableTilemap == null)
            {
                report.Error("Room Prefab", assetPath, "WalkableTilemap", "Required walkable tilemap is missing or not resolvable.", "Add FloorTilemap/WalkableTilemap or assign _walkableTilemap in RoomContext.");
            }

            Tilemap blockedTilemap = ResolveTilemap(roomContext, roomContextSerialized, "_blockedTilemap", "WallTilemap", "BlockedTilemap", "CollisionTilemap", "ObstacleTilemap");
            if (blockedTilemap == null)
            {
                if (isCombatRoom)
                {
                    report.Error("Room Prefab", assetPath, "BlockedTilemap", "Combat room is missing blocked/obstacle tilemap.", "Add WallTilemap/BlockedTilemap/CollisionTilemap or assign _blockedTilemap in RoomContext.");
                }
                else
                {
                    report.Warning("Room Prefab", assetPath, "BlockedTilemap", "Blocked tilemap could not be resolved.", "Add a blocked tilemap if the room uses obstacles or walls.");
                }
            }

            if (roomProfile == null)
            {
                if (isSpecialNonProceduralRoom)
                {
                    report.Info("Room Prefab", assetPath, nameof(RoomPrefabProfile), "RoomPrefabProfile is intentionally not required for this special non-procedural room.", "No action required unless this prefab is moved into the procedural room prototype set.");
                }
                else
                {
                    report.Error("Room Prefab", assetPath, nameof(RoomPrefabProfile), "RoomPrefabProfile is missing and room type cannot be resolved.", "Add RoomPrefabProfile to the room root or parent.");
                }
            }
            else if (!isSpecialNonProceduralRoom)
            {
                if (roomProfile.Doors == null || roomProfile.Doors.Count == 0)
                {
                    report.Warning("Room Prefab", assetPath, "Doors", "RoomPrefabProfile has no configured door anchors.", "Add entrances/exits if this room must connect procedurally.");
                }
                else
                {
                    for (int i = 0; i < roomProfile.Doors.Count; i++)
                    {
                        PDDoorConfig door = roomProfile.Doors[i];
                        if (door == null || door.AnchorTransform == null)
                        {
                            report.Error("Room Prefab", assetPath, "Doors", $"Door config #{i} has a missing anchor transform.", "Assign the anchor transform for each procedural door.");
                        }
                    }
                }
            }

            RoomCameraBounds cameraBounds = prefabRoot.GetComponentInChildren<RoomCameraBounds>(true);
            if (cameraBounds == null)
            {
                report.Warning("Room Prefab", assetPath, nameof(RoomCameraBounds), "RoomCameraBounds is missing.", "Add RoomCameraBounds if camera fitting should be deterministic.");
            }
            else
            {
                BoxCollider2D boundsCollider = cameraBounds.GetComponent<BoxCollider2D>();
                if (boundsCollider == null)
                {
                    report.Error("Room Prefab", assetPath, nameof(RoomCameraBounds), "RoomCameraBounds exists without BoxCollider2D.", "Add a trigger BoxCollider2D to the RoomCameraBounds object.");
                }
                else if (!boundsCollider.isTrigger)
                {
                    report.Warning("Room Prefab", assetPath, nameof(RoomCameraBounds), "RoomCameraBounds BoxCollider2D is not a trigger.", "Convert the collider to trigger to match the authoring contract.");
                }
            }

            Transform[] spawnTransforms = prefabRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != null && transform.name.IndexOf("spawn", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (spawnTransforms.Length == 0)
            {
                report.Warning("Room Prefab", assetPath, "SpawnPoints", "No explicit spawn points were found by name.", "If the room relies on grid-derived spawning only, document that; otherwise add explicit spawn markers.");
            }

            CombatRoomController combatController = ResolveCombatController(roomContext, roomContextSerialized);
            if (isCombatRoom)
            {
                ValidateCombatRoom(prefabRoot, assetPath, roomContext, combatController, roomGrid, blockedTilemap, report);
            }
            else if (combatController != null)
            {
                report.Warning("Room Prefab", assetPath, nameof(CombatRoomController), $"Non-combat room type '{roomProfile.RoomType}' contains CombatRoomController.", "Remove the combat controller or reclassify the room type.");
            }

            if (!report.HasBlockingIssues(assetPath))
                report.Pass(assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateCombatRoom(
        GameObject prefabRoot,
        string assetPath,
        RoomContext roomContext,
        CombatRoomController combatController,
        RoomGrid roomGrid,
        Tilemap blockedTilemap,
        PrototypeValidationReport report)
    {
        if (combatController == null)
        {
            report.Error("Combat Room", assetPath, nameof(CombatRoomController), "Combat room is missing CombatRoomController.", "Add or wire a CombatRoomController.");
        }

        if (roomGrid == null)
        {
            report.Error("Combat Room", assetPath, nameof(RoomGrid), "Combat room is missing RoomGrid.", "Add or wire a RoomGrid.");
        }

        if (blockedTilemap == null)
        {
            report.Error("Combat Room", assetPath, "ObstacleTilemap", "Combat room is missing blocked/obstacle tilemap.", "Add a blocked tilemap so walkability is defined before runtime.");
        }

        RoomCameraBounds cameraBounds = prefabRoot.GetComponentInChildren<RoomCameraBounds>(true);
        if (cameraBounds == null)
        {
            report.Error("Combat Room", assetPath, nameof(RoomCameraBounds), "Combat room has no camera bounds.", "Add RoomCameraBounds to make combat framing deterministic.");
        }

        Unit[] units = prefabRoot.GetComponentsInChildren<Unit>(true);
        int enemyUnits = units.Count(unit => unit != null && unit.Team == UnitTeam.Enemy);
        int allyUnits = units.Count(unit => unit != null && unit.Team == UnitTeam.Ally);

        Transform[] spawnLikeTransforms = prefabRoot.GetComponentsInChildren<Transform>(true)
            .Where(transform => transform != null && transform.name.IndexOf("spawn", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        if (enemyUnits == 0 && spawnLikeTransforms.All(transform => transform.name.IndexOf("enemy", StringComparison.OrdinalIgnoreCase) < 0))
        {
            report.Warning("Combat Room", assetPath, "EnemySpawn", "No enemy units or explicit enemy spawn markers were found.", "Add pre-authored enemies, spawn markers, or confirm RoomContentGenerator handles this room.");
        }

        if (allyUnits == 0 && spawnLikeTransforms.All(transform => transform.name.IndexOf("party", StringComparison.OrdinalIgnoreCase) < 0 &&
                                                                     transform.name.IndexOf("player", StringComparison.OrdinalIgnoreCase) < 0))
        {
            report.Info("Combat Room", assetPath, "PartySpawn", "No explicit party/player spawn markers were found. Current runtime may rely on RoomPartySpawner grid fallback.", "Add explicit markers only if you want deterministic deployment anchors.");
        }

        RoomContentGenerator contentGenerator = ResolveRoomContentGenerator(roomContext, new SerializedObject(roomContext));
        if (contentGenerator == null && enemyUnits == 0)
        {
            report.Warning("Combat Room", assetPath, nameof(RoomContentGenerator), "Combat room has no RoomContentGenerator and no pre-authored enemies.", "Add encounter content generation or author enemy units directly.");
        }

        RoomDoor[] roomDoors = prefabRoot.GetComponentsInChildren<RoomDoor>(true);
        if (roomDoors.Length == 0)
        {
            report.Warning("Combat Room", assetPath, nameof(RoomDoor), "Combat room has no RoomDoor components for exit flow.", "Add room doors or confirm the room is intentionally isolated.");
        }
    }

    private static void ValidateScenes(PrototypeValidationReport report)
    {
        ValidateScene(SafeZoneScenePath, report, ValidateSafeZoneScene);
        ValidateScene(DungeonScenePath, report, ValidateDungeonScene);
    }

    private static void ValidateScene(string scenePath, PrototypeValidationReport report, Action<Scene, PrototypeValidationReport> validationAction)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            report.Error("Scene", scenePath, "SceneAsset", "Scene asset could not be loaded.", "Restore the scene asset or update the validator path.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            validationAction(scene, report);
            if (!report.HasBlockingIssues(scenePath))
                report.Pass(scenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateSafeZoneScene(Scene scene, PrototypeValidationReport report)
    {
        GameSceneManager gameSceneManager = GetSceneComponents<GameSceneManager>(scene).FirstOrDefault();
        if (gameSceneManager == null)
        {
            report.Error("SafeZone", scene.path, nameof(GameSceneManager), "SafeZone scene is missing GameSceneManager.", "Add a GameSceneManager instance to the scene.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(gameSceneManager);
            ValidateSceneNameProperty(serialized, "_safeZoneSceneName", scene.path, "SafeZone", report);
            ValidateSceneNameProperty(serialized, "_dungeonSceneName", scene.path, "Dungeon", report);
        }

        DungeonEntranceInteraction[] entrances = GetSceneComponents<DungeonEntranceInteraction>(scene).ToArray();
        if (entrances.Length == 0)
        {
            report.Error("SafeZone", scene.path, nameof(DungeonEntranceInteraction), "SafeZone scene has no DungeonEntranceInteraction.", "Add at least one dungeon entrance interactable.");
        }

        for (int i = 0; i < entrances.Length; i++)
        {
            DungeonEntranceInteraction entrance = entrances[i];
            SerializedObject serialized = new SerializedObject(entrance);
            RoomGrid grid = serialized.FindProperty("_grid")?.objectReferenceValue as RoomGrid;
            if (grid == null)
            {
                grid = entrance.GetComponentInParent<RoomGrid>(true);
            }

            if (grid == null)
            {
                report.Error("SafeZone", scene.path, nameof(RoomGrid), $"Dungeon entrance '{entrance.name}' cannot resolve a RoomGrid.", "Parent the entrance under a room with RoomGrid or assign the _grid reference.");
            }
        }
    }

    private static void ValidateDungeonScene(Scene scene, PrototypeValidationReport report)
    {
        FloorManager floorManager = GetSceneComponents<FloorManager>(scene).FirstOrDefault();
        if (floorManager == null)
        {
            report.Error("Dungeon", scene.path, nameof(FloorManager), "Dungeon scene is missing FloorManager.", "Add FloorManager to the dungeon bootstrap scene.");
        }

        PrefabDungeonGenerator generator = GetSceneComponents<PrefabDungeonGenerator>(scene).FirstOrDefault();
        if (generator == null)
        {
            report.Error("Dungeon", scene.path, nameof(PrefabDungeonGenerator), "Dungeon scene is missing PrefabDungeonGenerator.", "Add PrefabDungeonGenerator to the dungeon bootstrap scene.");
        }

        GameSceneManager gameSceneManager = GetSceneComponents<GameSceneManager>(scene).FirstOrDefault();
        if (gameSceneManager == null)
        {
            report.Warning("Dungeon", scene.path, nameof(GameSceneManager), "No GameSceneManager was found while validating Dungeon scene.", "If the manager is expected to persist from SafeZone, verify scene load order manually.");
        }
    }

    private static void ValidateManagerPrefab(PrototypeValidationReport report)
    {
        string[] guids = AssetDatabase.FindAssets(ManagerPartySearch);
        if (guids.Length == 0)
        {
            report.Warning("Managers", "Assets/Core/Prefabs", "[Manager_Party]", "Manager party prefab was not found by name.", "If the manager prefab was renamed, update the validator search constant.");
            return;
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            ValidateMissingScripts(prefabRoot, assetPath, "Manager Prefab", report);

            NecromancerSpawner spawner = prefabRoot.GetComponent<NecromancerSpawner>();
            RoomPartySpawner roomPartySpawner = prefabRoot.GetComponent<RoomPartySpawner>();
            PartyDefeatReturnHandler returnHandler = prefabRoot.GetComponent<PartyDefeatReturnHandler>();

            if (spawner == null)
            {
                report.Error("Manager Prefab", assetPath, nameof(NecromancerSpawner), "Manager prefab is missing NecromancerSpawner.", "Add NecromancerSpawner to the manager prefab.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(spawner);
                if (serialized.FindProperty("_necromancerPrefab")?.objectReferenceValue == null)
                {
                    report.Error("Manager Prefab", assetPath, "Necromancer Prefab", "NecromancerSpawner has no necromancer prefab assigned.", "Assign the player necromancer prefab.");
                }

                if (serialized.FindProperty("_fallbackRoomContext")?.objectReferenceValue == null)
                {
                    report.Warning("Manager Prefab", assetPath, nameof(RoomContext), "NecromancerSpawner fallback RoomContext is not assigned.", "Leave it null only if all supported flows resolve the room procedurally at runtime.");
                }
            }

            if (roomPartySpawner == null)
            {
                report.Error("Manager Prefab", assetPath, nameof(RoomPartySpawner), "Manager prefab is missing RoomPartySpawner.", "Add RoomPartySpawner to the manager prefab.");
            }

            if (returnHandler == null)
            {
                report.Error("Manager Prefab", assetPath, nameof(PartyDefeatReturnHandler), "Manager prefab is missing PartyDefeatReturnHandler.", "Add PartyDefeatReturnHandler to support post-run return.");
            }

            if (!report.HasBlockingIssues(assetPath))
                report.Pass(assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateUnitDataAndCreaturePrefabs(PrototypeValidationReport report)
    {
        string[] unitDataGuids = AssetDatabase.FindAssets("t:UnitData");
        var validatedPrefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < unitDataGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(unitDataGuids[i]);
            UnitData unitData = AssetDatabase.LoadAssetAtPath<UnitData>(assetPath);
            if (unitData == null)
                continue;

            ValidateUnitData(assetPath, unitData, report);

            if (unitData.unitPrefab == null)
                continue;

            string prefabPath = AssetDatabase.GetAssetPath(unitData.unitPrefab);
            if (!ShouldValidateAsProductionAsset(prefabPath) ||
                string.IsNullOrWhiteSpace(prefabPath) ||
                !validatedPrefabPaths.Add(prefabPath))
                continue;

            ValidateCreaturePrefab(prefabPath, unitData, report);
        }
    }

    private static void ValidateUnitData(string assetPath, UnitData unitData, PrototypeValidationReport report)
    {
        if (!ShouldValidateAsProductionAsset(assetPath))
            return;

        if (unitData.unitPrefab == null)
        {
            report.Error("UnitData", assetPath, "unitPrefab", $"UnitData '{unitData.name}' has no prefab assigned.", "Assign a creature prefab.");
        }

        if (unitData.stats == null)
        {
            report.Error("UnitData", assetPath, "stats", $"UnitData '{unitData.name}' has no stats block.", "Assign runtime stats.");
        }

        if (unitData.skill != null)
        {
            List<string> issues = SkillProductionSupportCatalog.GetProductionSupportIssues(unitData.skill);
            for (int i = 0; i < issues.Count; i++)
            {
                report.Error("Skill Reference", assetPath, nameof(SkillData), $"UnitData '{unitData.name}' references non-production skill '{unitData.skill.name}': {issues[i]}", "Use a production-supported skill composition.");
            }
        }

        if (unitData.AudioSet == null)
        {
            report.RecordProductionUnitDataMissingAudioSet(assetPath, unitData.name);
        }

        if (!report.HasBlockingIssues(assetPath))
            report.Pass(assetPath);
    }

    private static bool ShouldValidateAsProductionAsset(string assetPath)
    {
        return !IsNonProductionAssetPath(assetPath);
    }

    private static bool IsNonProductionAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        return IsDebugOrTestAsset(assetPath) ||
               ContainsPathToken(assetPath, "/_Excluded/") ||
               ContainsPathToken(assetPath, "/_LabTemp/");
    }

    private static bool IsDebugOrTestAsset(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        string normalizedPath = assetPath.Replace('\\', '/');
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedPath);

        return ContainsPathToken(normalizedPath, "/Testing/") ||
               ContainsPathToken(normalizedPath, "/TestPrefabs/") ||
               fileNameWithoutExtension.StartsWith("TEST_", StringComparison.OrdinalIgnoreCase) ||
               fileNameWithoutExtension.IndexOf("_Debug", StringComparison.OrdinalIgnoreCase) >= 0 ||
               fileNameWithoutExtension.IndexOf("Debug_", StringComparison.OrdinalIgnoreCase) >= 0 ||
               fileNameWithoutExtension.IndexOf("DebugData", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ContainsPathToken(string assetPath, string token)
    {
        return assetPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsSpecialNonProceduralRoomPrefab(string assetPath)
    {
        return string.Equals(assetPath, SafeZoneRoomPrefabPath, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCreaturePrefab(string assetPath, UnitData unitData, PrototypeValidationReport report)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            ValidateMissingScripts(prefabRoot, assetPath, "Creature Prefab", report);

            Unit unit = prefabRoot.GetComponent<Unit>() ?? prefabRoot.GetComponentInChildren<Unit>(true);
            if (unit == null)
            {
                report.Error("Creature Prefab", assetPath, nameof(Unit), "Creature prefab is missing Unit.", "Add Unit to the root creature object.");
                return;
            }

            SerializedObject unitSerialized = new SerializedObject(unit);
            UnitData embeddedUnitData = unitSerialized.FindProperty("_unitData")?.objectReferenceValue as UnitData;
            if (embeddedUnitData == null)
            {
                report.Error("Creature Prefab", assetPath, nameof(UnitData), "Unit component has no embedded UnitData assigned.", "Assign the UnitData reference on the prefab.");
            }
            else if (unitData != null && embeddedUnitData != unitData)
            {
                report.Warning("Creature Prefab", assetPath, nameof(UnitData), $"UnitData mismatch. Prefab embeds '{embeddedUnitData.name}' but asset validation came from '{unitData.name}'.", "Align the prefab UnitData assignment with the productivo UnitData asset.");
            }

            if (unit.GetComponent<LifeController>() == null)
                report.Error("Creature Prefab", assetPath, nameof(LifeController), "Creature prefab is missing LifeController.", "Add LifeController.");

            if (unit.GetComponent<UnitCombat>() == null)
                report.Error("Creature Prefab", assetPath, nameof(UnitCombat), "Creature prefab is missing UnitCombat.", "Add UnitCombat.");

            if (unit.GetComponent<UnitBrain>() == null)
                report.Error("Creature Prefab", assetPath, nameof(UnitBrain), "Creature prefab is missing UnitBrain.", "Add UnitBrain.");

            if (unit.GetComponent<UnitMovement>() == null)
                report.Error("Creature Prefab", assetPath, nameof(UnitMovement), "Creature prefab is missing UnitMovement.", "Add UnitMovement.");

            SkillCaster skillCaster = unit.GetComponent<SkillCaster>();
            if (unitData != null && unitData.skill != null && skillCaster == null)
            {
                report.Error("Creature Prefab", assetPath, nameof(SkillCaster), $"UnitData '{unitData.name}' has a skill assigned but the prefab has no SkillCaster.", "Add SkillCaster or remove the skill reference.");
            }

            if (unit.GetComponentInChildren<Animator>(true) == null)
            {
                report.Warning("Creature Prefab", assetPath, nameof(Animator), "Creature prefab has no Animator.", "Add Animator if the current content expects animated feedback. This is non-blocking.");
            }

            if (!report.HasBlockingIssues(assetPath))
                report.Pass(assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateSceneNameProperty(SerializedObject serializedObject, string propertyName, string scenePath, string label, PrototypeValidationReport report)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        string sceneName = property != null ? property.stringValue : null;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            report.Error("Scene Flow", scenePath, propertyName, $"{label} scene name is empty in GameSceneManager.", "Assign the scene name used by the current flow.");
            return;
        }

        string[] sceneGuids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
        if (sceneGuids.Length == 0)
        {
            report.Error("Scene Flow", scenePath, propertyName, $"{label} scene '{sceneName}' does not exist as a project scene asset.", "Create or rename the scene asset to match the manager configuration.");
        }
    }

    private static void ValidateMissingScripts(GameObject root, string assetPath, string category, PrototypeValidationReport report)
    {
        int missingScriptCount = 0;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);

        if (missingScriptCount > 0)
        {
            report.Error(category, assetPath, "MissingScript", $"Asset contains {missingScriptCount} missing script reference(s).", "Restore the script or remove the broken component.");
        }
    }

    private static IEnumerable<T> GetSceneComponents<T>(Scene scene)
        where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T[] components = roots[i].GetComponentsInChildren<T>(true);
            for (int j = 0; j < components.Length; j++)
                yield return components[j];
        }
    }

    private static void ValidateDuplicateComponents<T>(GameObject root, string assetPath, string category, PrototypeValidationReport report, bool dangerous)
        where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        if (components.Length <= 1)
            return;

        if (dangerous)
        {
            report.Error(category, assetPath, typeof(T).Name, $"Found {components.Length} {typeof(T).Name} components.", "Keep a single authoritative component instance.");
        }
        else
        {
            report.Warning(category, assetPath, typeof(T).Name, $"Found {components.Length} {typeof(T).Name} components.", "Confirm the duplicates are intentional.");
        }
    }

    private static RoomGrid ResolveRoomGrid(RoomContext roomContext, SerializedObject serializedObject)
    {
        RoomGrid roomGrid = serializedObject.FindProperty("_roomGrid")?.objectReferenceValue as RoomGrid;
        return roomGrid != null ? roomGrid : roomContext.GetComponentInChildren<RoomGrid>(true);
    }

    private static RoomPrefabProfile ResolveRoomProfile(RoomContext roomContext, SerializedObject serializedObject)
    {
        RoomPrefabProfile roomProfile = serializedObject.FindProperty("_roomProfile")?.objectReferenceValue as RoomPrefabProfile;
        if (roomProfile != null)
            return roomProfile;

        roomProfile = roomContext.GetComponent<RoomPrefabProfile>();
        return roomProfile != null ? roomProfile : roomContext.GetComponentInParent<RoomPrefabProfile>(true);
    }

    private static CombatRoomController ResolveCombatController(RoomContext roomContext, SerializedObject serializedObject)
    {
        CombatRoomController combatController = serializedObject.FindProperty("_combatController")?.objectReferenceValue as CombatRoomController;
        if (combatController != null)
            return combatController;

        combatController = roomContext.GetComponent<CombatRoomController>();
        return combatController != null ? combatController : roomContext.GetComponentInChildren<CombatRoomController>(true);
    }

    private static RoomContentGenerator ResolveRoomContentGenerator(RoomContext roomContext, SerializedObject serializedObject)
    {
        RoomContentGenerator contentGenerator = serializedObject.FindProperty("_contentGenerator")?.objectReferenceValue as RoomContentGenerator;
        return contentGenerator != null ? contentGenerator : roomContext.GetComponentInChildren<RoomContentGenerator>(true);
    }

    private static Tilemap ResolveTilemap(RoomContext roomContext, SerializedObject serializedObject, string propertyName, params string[] names)
    {
        Tilemap tilemap = serializedObject.FindProperty(propertyName)?.objectReferenceValue as Tilemap;
        if (tilemap != null)
            return tilemap;

        Tilemap[] tilemaps = roomContext.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap candidate = tilemaps[i];
            if (candidate == null)
                continue;

            for (int j = 0; j < names.Length; j++)
            {
                if (candidate.name.Equals(names[j], StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }

        return null;
    }

    private enum IssueSeverity
    {
        Error,
        Warning,
        Info
    }

    private sealed class PrototypeValidationIssue
    {
        public PrototypeValidationIssue(IssueSeverity severity, string category, string assetPath, string component, string message, string suggestion)
        {
            Severity = severity;
            Category = category;
            AssetPath = assetPath;
            Component = component;
            Message = message;
            Suggestion = suggestion;
        }

        public IssueSeverity Severity { get; }
        public string Category { get; }
        public string AssetPath { get; }
        public string Component { get; }
        public string Message { get; }
        public string Suggestion { get; }
    }

    private sealed class PrototypeValidationReport
    {
        private static readonly string[] MarkdownCategoryOrder =
        {
            "Build Settings",
            "Rooms",
            "Combat Rooms",
            "SafeZone / Dungeon",
            "Party Manager",
            "Creature Prefabs",
            "UnitData",
            "Skill References"
        };

        private readonly List<PrototypeValidationIssue> _issues = new();
        private readonly HashSet<string> _passedAssets = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _failedAssets = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _productionUnitDataMissingAudioSet = new();

        public IReadOnlyCollection<string> PassedAssets => _passedAssets;
        public IReadOnlyCollection<string> FailedAssets => _failedAssets;
        public int ErrorCount => _issues.Count(issue => issue.Severity == IssueSeverity.Error);
        public int WarningCount => _issues.Count(issue => issue.Severity == IssueSeverity.Warning);
        public int InfoCount => _issues.Count(issue => issue.Severity == IssueSeverity.Info);

        public void Error(string category, string assetPath, string component, string message, string suggestion)
        {
            AddIssue(IssueSeverity.Error, category, assetPath, component, message, suggestion);
        }

        public void Warning(string category, string assetPath, string component, string message, string suggestion)
        {
            AddIssue(IssueSeverity.Warning, category, assetPath, component, message, suggestion);
        }

        public void Info(string category, string assetPath, string component, string message, string suggestion)
        {
            AddIssue(IssueSeverity.Info, category, assetPath, component, message, suggestion);
        }

        public void Pass(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || _failedAssets.Contains(assetPath))
                return;

            _passedAssets.Add(assetPath);
        }

        public bool HasBlockingIssues(string assetPath)
        {
            return _issues.Any(issue =>
                issue.Severity == IssueSeverity.Error &&
                string.Equals(issue.AssetPath, assetPath, StringComparison.OrdinalIgnoreCase));
        }

        public void RecordProductionUnitDataMissingAudioSet(string assetPath, string unitDataName)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            _productionUnitDataMissingAudioSet.Add($"{unitDataName} ({assetPath})");
        }

        public void FinalizeAssetBuckets()
        {
            FinalizeAggregatedIssues();

            for (int i = 0; i < _issues.Count; i++)
            {
                PrototypeValidationIssue issue = _issues[i];
                if (string.IsNullOrWhiteSpace(issue.AssetPath))
                    continue;

                if (issue.Severity == IssueSeverity.Error)
                {
                    _failedAssets.Add(issue.AssetPath);
                    _passedAssets.Remove(issue.AssetPath);
                }
            }
        }

        public void LogToConsole()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[PrototypeReadiness] Validation completed.");
            builder.AppendLine($"[PrototypeReadiness] Errors: {ErrorCount} | Warnings: {WarningCount} | Info: {InfoCount}");
            builder.AppendLine($"[PrototypeReadiness] Failed Assets: {_failedAssets.Count} | Passed Assets: {_passedAssets.Count}");
            builder.AppendLine($"[PrototypeReadiness] Markdown Report: {MarkdownReportPath}");

            AppendIssuesForSeverity(builder, IssueSeverity.Error);
            AppendIssuesForSeverity(builder, IssueSeverity.Warning);
            AppendIssuesForSeverity(builder, IssueSeverity.Info);
            AppendAssetList(builder, "Failing Assets", _failedAssets);
            AppendAssetList(builder, "Passing Assets", _passedAssets);

            if (ErrorCount > 0)
                Debug.LogError(builder.ToString());
            else if (WarningCount > 0)
                Debug.LogWarning(builder.ToString());
            else
                Debug.Log(builder.ToString());
        }

        public void WriteMarkdownReport(string reportPath)
        {
            string fullReportPath = Path.GetFullPath(reportPath);
            string directoryPath = Path.GetDirectoryName(fullReportPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(fullReportPath, BuildMarkdownReport(), Encoding.UTF8);
        }

        private void AddIssue(IssueSeverity severity, string category, string assetPath, string component, string message, string suggestion)
        {
            _issues.Add(new PrototypeValidationIssue(severity, category, assetPath, component, message, suggestion));

            if (!string.IsNullOrWhiteSpace(assetPath) && severity == IssueSeverity.Error)
            {
                _failedAssets.Add(assetPath);
                _passedAssets.Remove(assetPath);
            }
        }

        private void FinalizeAggregatedIssues()
        {
            if (_productionUnitDataMissingAudioSet.Count == 0)
                return;

            string[] orderedEntries = _productionUnitDataMissingAudioSet
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            AddIssue(
                IssueSeverity.Info,
                "UnitData",
                "Multiple production UnitData assets",
                "AudioSet",
                $"{orderedEntries.Length} production UnitData assets are missing AudioSet: {string.Join("; ", orderedEntries)}",
                "Assign AudioSet only for production units that need combat feedback.");

            _productionUnitDataMissingAudioSet.Clear();
        }

        private void AppendIssuesForSeverity(StringBuilder builder, IssueSeverity severity)
        {
            List<PrototypeValidationIssue> scopedIssues = _issues
                .Where(issue => issue.Severity == severity)
                .OrderBy(issue => issue.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(issue => issue.Component, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (scopedIssues.Count == 0)
                return;

            builder.AppendLine();
            builder.AppendLine($"[{severity}]");
            for (int i = 0; i < scopedIssues.Count; i++)
            {
                PrototypeValidationIssue issue = scopedIssues[i];
                builder.AppendLine(
                    $"- [{issue.Category}] {issue.AssetPath} | Component: {issue.Component} | {issue.Message} | Suggested Action: {issue.Suggestion}");
            }
        }

        private static void AppendAssetList(StringBuilder builder, string title, IEnumerable<string> assets)
        {
            string[] orderedAssets = assets
                .Where(asset => !string.IsNullOrWhiteSpace(asset))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(asset => asset, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (orderedAssets.Length == 0)
                return;

            builder.AppendLine();
            builder.AppendLine($"[{title}]");
            for (int i = 0; i < orderedAssets.Length; i++)
                builder.AppendLine($"- {orderedAssets[i]}");
        }

        private string BuildMarkdownReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Prototype Readiness Report");
            builder.AppendLine();
            builder.AppendLine($"- Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            builder.AppendLine($"- Result: {(ErrorCount > 0 ? "FAIL" : "PASS")}");
            builder.AppendLine($"- Errors: {ErrorCount}");
            builder.AppendLine($"- Warnings: {WarningCount}");
            builder.AppendLine($"- Info: {InfoCount}");
            builder.AppendLine($"- Failed Assets: {_failedAssets.Count}");
            builder.AppendLine($"- Passed Assets: {_passedAssets.Count}");

            for (int i = 0; i < MarkdownCategoryOrder.Length; i++)
            {
                string markdownCategory = MarkdownCategoryOrder[i];
                AppendMarkdownCategory(builder, markdownCategory);
            }

            AppendMarkdownAssetList(builder, "Failing Assets", _failedAssets);
            AppendMarkdownAssetList(builder, "Passing Assets", _passedAssets);
            return builder.ToString();
        }

        private void AppendMarkdownCategory(StringBuilder builder, string markdownCategory)
        {
            List<PrototypeValidationIssue> categoryIssues = _issues
                .Where(issue => string.Equals(MapToMarkdownCategory(issue.Category), markdownCategory, StringComparison.Ordinal))
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(issue => issue.Component, StringComparer.OrdinalIgnoreCase)
                .ToList();

            builder.AppendLine();
            builder.AppendLine($"## {markdownCategory}");

            if (categoryIssues.Count == 0)
            {
                builder.AppendLine();
                builder.AppendLine("_No issues._");
                return;
            }

            for (int i = 0; i < categoryIssues.Count; i++)
            {
                PrototypeValidationIssue issue = categoryIssues[i];
                builder.AppendLine();
                builder.AppendLine($"### {issue.Severity}");
                builder.AppendLine($"- Severity: {issue.Severity}");
                builder.AppendLine($"- Category: {issue.Category}");
                builder.AppendLine($"- Path: {issue.AssetPath}");
                builder.AppendLine($"- Component: {issue.Component}");
                builder.AppendLine($"- Message: {issue.Message}");
                builder.AppendLine($"- Suggestion: {FormatMarkdownValue(issue.Suggestion)}");
            }
        }

        private static void AppendMarkdownAssetList(StringBuilder builder, string title, IEnumerable<string> assets)
        {
            string[] orderedAssets = assets
                .Where(asset => !string.IsNullOrWhiteSpace(asset))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(asset => asset, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            builder.AppendLine();
            builder.AppendLine($"## {title}");
            builder.AppendLine();

            if (orderedAssets.Length == 0)
            {
                builder.AppendLine("_None._");
                return;
            }

            for (int i = 0; i < orderedAssets.Length; i++)
                builder.AppendLine($"- {orderedAssets[i]}");
        }

        private static string MapToMarkdownCategory(string category)
        {
            return category switch
            {
                "BuildSettings" => "Build Settings",
                "Room Prefab" => "Rooms",
                "Combat Room" => "Combat Rooms",
                "SafeZone" => "SafeZone / Dungeon",
                "Dungeon" => "SafeZone / Dungeon",
                "Scene" => "SafeZone / Dungeon",
                "Scene Flow" => "SafeZone / Dungeon",
                "Manager Prefab" => "Party Manager",
                "Creature Prefab" => "Creature Prefabs",
                "UnitData" => "UnitData",
                "Skill Reference" => "Skill References",
                _ => category
            };
        }

        private static string FormatMarkdownValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }
    }
}
