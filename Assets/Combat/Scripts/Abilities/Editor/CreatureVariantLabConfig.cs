using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureVariantLabConfig", menuName = "Combat/Editor/Creature Variant Lab Config")]
public class CreatureVariantLabConfig : ScriptableObject
{
    [Serializable]
    public class FactionRolePreset
    {
        public UnitFaction faction;
        public UnitRole role;
        [UnityEngine.Serialization.FormerlySerializedAs("defaultPrefab")]
        public GameObject prefabOverride;
        public UnitCombatStyle defaultCombatStyle = UnitCombatStyle.Default;
        public UnitTargetingMode defaultTargetingMode = UnitTargetingMode.RolePriority;
        public int defaultMaxHealth = 80;
        public int defaultAttackDamage = 12;
        public float defaultAttackCooldown = 1f;
        public int defaultAttackRangeInCells = 1;
        public int defaultPreferredDistanceInCells = 1;
        public float defaultAccuracy = 0.85f;
        public float defaultEvasion = 0.1f;
        public int defaultDefense = 0;
        public float defaultMoveSpeed = 3.5f;
        public float defaultVisionRange = 8f;
        public int defaultManaCostToRecruit = 1;
        public int defaultManaCostToAbsorbSoul = 1;
    }

    [Serializable]
    public class MaxTargetsConfig
    {
        public int direct = 1;
        public int line = 10;
        public int area = 10;
        public int fallback = 10;
    }

    [Header("Creation Presets")]
    public FactionRolePreset[] presets = new FactionRolePreset[0];

    [Header("Debug")]
    [UnityEngine.Serialization.FormerlySerializedAs("dummyPrefab")]
    public GameObject dummyPrefabOverride;

    [Header("Max Targets by Delivery")]
    public MaxTargetsConfig maxTargets = new MaxTargetsConfig();

    [Header("Paths")]
    public string tempAssetFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/_LabTemp";
    public string productionSkillFolder = "Assets/Core/Data/Scriptable Objects/Combat/Skills";
    public string productionVariantFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated";
    public string testScenePath = "Assets/Core/Scenes/TestMapScene.unity";

    public FactionRolePreset GetPreset(UnitFaction faction, UnitRole role)
    {
        if (presets == null)
            return null;

        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i] != null && presets[i].faction == faction && presets[i].role == role)
                return presets[i];
        }
        return null;
    }

    public GameObject GetPrefab(UnitFaction faction, UnitRole role)
    {
        var preset = GetPreset(faction, role);
        return preset != null ? preset.prefabOverride : null;
    }

    public string GetProductionSkillFolder(UnitRole role)
    {
        return $"{productionSkillFolder}/Role_{role}";
    }

    public string GetProductionVariantFolder(UnitFaction faction, UnitRole role)
    {
        return $"{productionVariantFolder}/{faction}/{role}";
    }

    public static FactionRolePreset GetDefaultPreset(UnitFaction faction, UnitRole role)
    {
        return role switch
        {
            UnitRole.DPS => new FactionRolePreset
            {
                faction = faction,
                role = UnitRole.DPS,
                defaultCombatStyle = UnitCombatStyle.Default,
                defaultMaxHealth = 80,
                defaultAttackDamage = 12,
                defaultAttackCooldown = 1f,
                defaultAttackRangeInCells = 1,
                defaultPreferredDistanceInCells = 1,
                defaultAccuracy = 0.85f,
                defaultEvasion = 0.1f,
                defaultDefense = 0,
                defaultMoveSpeed = 3.5f,
                defaultVisionRange = 8f,
                defaultManaCostToRecruit = 1,
                defaultManaCostToAbsorbSoul = 1,
            },
            UnitRole.Tank => new FactionRolePreset
            {
                faction = faction,
                role = UnitRole.Tank,
                defaultCombatStyle = UnitCombatStyle.Melee,
                defaultMaxHealth = 150,
                defaultAttackDamage = 6,
                defaultAttackCooldown = 1.2f,
                defaultAttackRangeInCells = 1,
                defaultPreferredDistanceInCells = 1,
                defaultAccuracy = 0.8f,
                defaultEvasion = 0.05f,
                defaultDefense = 8,
                defaultMoveSpeed = 2.5f,
                defaultVisionRange = 8f,
                defaultManaCostToRecruit = 2,
                defaultManaCostToAbsorbSoul = 2,
            },
            UnitRole.Support => new FactionRolePreset
            {
                faction = faction,
                role = UnitRole.Support,
                defaultCombatStyle = UnitCombatStyle.Ranged,
                defaultMaxHealth = 90,
                defaultAttackDamage = 4,
                defaultAttackCooldown = 1.4f,
                defaultAttackRangeInCells = 3,
                defaultPreferredDistanceInCells = 3,
                defaultAccuracy = 0.85f,
                defaultEvasion = 0.1f,
                defaultDefense = 2,
                defaultMoveSpeed = 3f,
                defaultVisionRange = 8f,
                defaultManaCostToRecruit = 2,
                defaultManaCostToAbsorbSoul = 2,
            },
            _ => new FactionRolePreset
            {
                faction = faction,
                role = role,
                defaultCombatStyle = UnitCombatStyle.Default,
                defaultMaxHealth = 80,
                defaultAttackDamage = 12,
                defaultAttackCooldown = 1f,
                defaultAttackRangeInCells = 1,
                defaultPreferredDistanceInCells = 1,
                defaultAccuracy = 0.85f,
                defaultEvasion = 0.1f,
                defaultDefense = 0,
                defaultMoveSpeed = 3.5f,
                defaultVisionRange = 8f,
                defaultManaCostToRecruit = 1,
                defaultManaCostToAbsorbSoul = 1,
            }
        };
    }

    public static readonly (UnitFaction faction, UnitRole role)[] RequiredPresets = new[]
    {
        (UnitFaction.Human, UnitRole.DPS),
        (UnitFaction.Human, UnitRole.Tank),
        (UnitFaction.Human, UnitRole.Support),
        (UnitFaction.Orc, UnitRole.DPS),
        (UnitFaction.Orc, UnitRole.Tank),
        (UnitFaction.Orc, UnitRole.Support),
    };
}