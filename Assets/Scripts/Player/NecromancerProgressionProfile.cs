using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NecromancerLevelThreshold
{
    [Min(1)] public int Level;
    [Min(0)] public int RequiredTotalExperience;
}

[Serializable]
public struct NecromancerPartyCapacityThreshold
{
    [Min(1)] public int Level;
    [Min(1)] public int MaxPartyMembers;
}

[CreateAssetMenu(fileName = "NecromancerProgressionProfile", menuName = "Player/Necromancer Progression Profile")]
public class NecromancerProgressionProfile : ScriptableObject
{
    [Header("Starting State")]
    [SerializeField, Min(1)] private int _startingLevel = 1;
    [SerializeField, Min(0)] private int _startingTotalExperience;

    [Header("Hard Caps")]
    [SerializeField, Min(1)] private int _maximumPartyMembers = 18;
    [SerializeField] private bool _stopLevelProgressionAtMaxPartyCapacity = true;

    [Header("Room Victory Formula")]
    [SerializeField, Min(0)] private int _roomVictoryBaseExperience = 10;
    [SerializeField, Min(0)] private int _experiencePerEnemyDefeated = 15;
    [SerializeField, Min(0)] private int _experiencePerFloor = 5;

    [Header("Level Thresholds")]
    [SerializeField] private List<NecromancerLevelThreshold> _levelThresholds = new()
    {
        new NecromancerLevelThreshold { Level = 1, RequiredTotalExperience = 0 },
        new NecromancerLevelThreshold { Level = 2, RequiredTotalExperience = 100 },
        new NecromancerLevelThreshold { Level = 3, RequiredTotalExperience = 250 },
        new NecromancerLevelThreshold { Level = 4, RequiredTotalExperience = 450 },
        new NecromancerLevelThreshold { Level = 5, RequiredTotalExperience = 700 },
        new NecromancerLevelThreshold { Level = 6, RequiredTotalExperience = 1000 },
        new NecromancerLevelThreshold { Level = 7, RequiredTotalExperience = 1350 },
        new NecromancerLevelThreshold { Level = 8, RequiredTotalExperience = 1750 },
        new NecromancerLevelThreshold { Level = 9, RequiredTotalExperience = 2200 },
        new NecromancerLevelThreshold { Level = 10, RequiredTotalExperience = 2700 },
        new NecromancerLevelThreshold { Level = 11, RequiredTotalExperience = 3250 },
        new NecromancerLevelThreshold { Level = 12, RequiredTotalExperience = 3850 },
        new NecromancerLevelThreshold { Level = 13, RequiredTotalExperience = 4500 },
        new NecromancerLevelThreshold { Level = 14, RequiredTotalExperience = 5200 },
        new NecromancerLevelThreshold { Level = 15, RequiredTotalExperience = 5950 }
    };

    [Header("Party Capacity By Level")]
    [SerializeField] private List<NecromancerPartyCapacityThreshold> _partyCapacityThresholds = new()
    {
        new NecromancerPartyCapacityThreshold { Level = 1, MaxPartyMembers = 4 },
        new NecromancerPartyCapacityThreshold { Level = 2, MaxPartyMembers = 5 },
        new NecromancerPartyCapacityThreshold { Level = 3, MaxPartyMembers = 6 },
        new NecromancerPartyCapacityThreshold { Level = 4, MaxPartyMembers = 7 },
        new NecromancerPartyCapacityThreshold { Level = 5, MaxPartyMembers = 8 },
        new NecromancerPartyCapacityThreshold { Level = 6, MaxPartyMembers = 9 },
        new NecromancerPartyCapacityThreshold { Level = 7, MaxPartyMembers = 10 },
        new NecromancerPartyCapacityThreshold { Level = 8, MaxPartyMembers = 11 },
        new NecromancerPartyCapacityThreshold { Level = 9, MaxPartyMembers = 12 },
        new NecromancerPartyCapacityThreshold { Level = 10, MaxPartyMembers = 13 },
        new NecromancerPartyCapacityThreshold { Level = 11, MaxPartyMembers = 14 },
        new NecromancerPartyCapacityThreshold { Level = 12, MaxPartyMembers = 15 },
        new NecromancerPartyCapacityThreshold { Level = 13, MaxPartyMembers = 16 },
        new NecromancerPartyCapacityThreshold { Level = 14, MaxPartyMembers = 17 },
        new NecromancerPartyCapacityThreshold { Level = 15, MaxPartyMembers = 18 }
    };

    public int StartingLevel => Mathf.Max(1, _startingLevel);
    public int StartingTotalExperience => Mathf.Max(0, _startingTotalExperience);
    public int MaximumPartyMembers => Mathf.Max(1, _maximumPartyMembers);

    public int CalculateRoomVictoryExperience(int defeatedEnemies, int floorNumber)
    {
        int safeEnemyCount = Mathf.Max(0, defeatedEnemies);
        int safeFloorNumber = Mathf.Max(1, floorNumber);

        int experienceAward =
            _roomVictoryBaseExperience +
            (safeEnemyCount * _experiencePerEnemyDefeated) +
            (safeFloorNumber * _experiencePerFloor);

        return Mathf.Max(0, experienceAward);
    }

    public int GetLevelForExperience(int totalExperience)
    {
        int safeExperience = Mathf.Max(0, totalExperience);
        int resolvedLevel = StartingLevel;
        List<NecromancerLevelThreshold> sortedThresholds = GetSortedThresholds();

        for (int i = 0; i < sortedThresholds.Count; i++)
        {
            NecromancerLevelThreshold threshold = sortedThresholds[i];
            if (safeExperience < threshold.RequiredTotalExperience)
                break;

            resolvedLevel = Mathf.Max(resolvedLevel, threshold.Level);
        }

        return Mathf.Min(resolvedLevel, GetMaximumLevel());
    }

    public int GetRequiredTotalExperienceForLevel(int level)
    {
        int safeLevel = Mathf.Clamp(level, 1, GetMaximumLevel());
        int requiredExperience = safeLevel <= StartingLevel ? StartingTotalExperience : 0;
        List<NecromancerLevelThreshold> sortedThresholds = GetSortedThresholds();

        for (int i = 0; i < sortedThresholds.Count; i++)
        {
            NecromancerLevelThreshold threshold = sortedThresholds[i];
            if (threshold.Level > safeLevel)
                break;

            requiredExperience = threshold.RequiredTotalExperience;
            if (threshold.Level == safeLevel)
                break;
        }

        return Mathf.Max(0, requiredExperience);
    }

    public bool TryGetNextLevel(int currentLevel, out int nextLevel, out int requiredTotalExperience)
    {
        int maximumLevel = GetMaximumLevel();
        int safeLevel = Mathf.Clamp(currentLevel, 1, maximumLevel);
        if (safeLevel >= maximumLevel)
        {
            nextLevel = maximumLevel;
            requiredTotalExperience = GetRequiredTotalExperienceForLevel(maximumLevel);
            return false;
        }

        List<NecromancerLevelThreshold> sortedThresholds = GetSortedThresholds();

        for (int i = 0; i < sortedThresholds.Count; i++)
        {
            NecromancerLevelThreshold threshold = sortedThresholds[i];
            if (threshold.Level <= safeLevel)
                continue;

            nextLevel = threshold.Level;
            requiredTotalExperience = threshold.RequiredTotalExperience;
            return true;
        }

        nextLevel = safeLevel;
        requiredTotalExperience = GetRequiredTotalExperienceForLevel(safeLevel);
        return false;
    }

    public int GetMaxPartyMembersForLevel(int level, int fallbackMaxPartyMembers = 1)
    {
        int resolvedCapacity = Mathf.Max(1, fallbackMaxPartyMembers);
        int safeLevel = Mathf.Clamp(level, 1, GetMaximumLevel());
        List<NecromancerPartyCapacityThreshold> sortedThresholds = GetSortedPartyCapacityThresholds();

        for (int i = 0; i < sortedThresholds.Count; i++)
        {
            NecromancerPartyCapacityThreshold threshold = sortedThresholds[i];
            if (safeLevel < threshold.Level)
                break;

            resolvedCapacity = Mathf.Max(resolvedCapacity, threshold.MaxPartyMembers);
        }

        return Mathf.Min(resolvedCapacity, MaximumPartyMembers);
    }

    public int GetMaximumLevel()
    {
        List<NecromancerLevelThreshold> sortedThresholds = GetSortedThresholds();
        int maximumLevelFromExperience = StartingLevel;

        for (int i = 0; i < sortedThresholds.Count; i++)
            maximumLevelFromExperience = Mathf.Max(maximumLevelFromExperience, sortedThresholds[i].Level);

        if (!_stopLevelProgressionAtMaxPartyCapacity)
            return maximumLevelFromExperience;

        List<NecromancerPartyCapacityThreshold> sortedCapacityThresholds = GetSortedPartyCapacityThresholds();
        for (int i = 0; i < sortedCapacityThresholds.Count; i++)
        {
            NecromancerPartyCapacityThreshold threshold = sortedCapacityThresholds[i];
            if (threshold.MaxPartyMembers < MaximumPartyMembers)
                continue;

            return Mathf.Min(maximumLevelFromExperience, Mathf.Max(StartingLevel, threshold.Level));
        }

        return maximumLevelFromExperience;
    }

    public int GetMaximumTotalExperience()
    {
        return GetRequiredTotalExperienceForLevel(GetMaximumLevel());
    }

    private List<NecromancerLevelThreshold> GetSortedThresholds()
    {
        var sortedThresholds = new List<NecromancerLevelThreshold>(_levelThresholds.Count);

        for (int i = 0; i < _levelThresholds.Count; i++)
        {
            NecromancerLevelThreshold threshold = _levelThresholds[i];
            if (threshold.Level <= 0)
                continue;

            sortedThresholds.Add(new NecromancerLevelThreshold
            {
                Level = threshold.Level,
                RequiredTotalExperience = Mathf.Max(0, threshold.RequiredTotalExperience)
            });
        }

        sortedThresholds.Sort(CompareThresholds);
        return sortedThresholds;
    }

    private static int CompareThresholds(NecromancerLevelThreshold left, NecromancerLevelThreshold right)
    {
        int levelComparison = left.Level.CompareTo(right.Level);
        if (levelComparison != 0)
            return levelComparison;

        return left.RequiredTotalExperience.CompareTo(right.RequiredTotalExperience);
    }

    private List<NecromancerPartyCapacityThreshold> GetSortedPartyCapacityThresholds()
    {
        var sortedThresholds = new List<NecromancerPartyCapacityThreshold>(_partyCapacityThresholds.Count);

        for (int i = 0; i < _partyCapacityThresholds.Count; i++)
        {
            NecromancerPartyCapacityThreshold threshold = _partyCapacityThresholds[i];
            if (threshold.Level <= 0 || threshold.MaxPartyMembers <= 0)
                continue;

            sortedThresholds.Add(new NecromancerPartyCapacityThreshold
            {
                Level = threshold.Level,
                MaxPartyMembers = threshold.MaxPartyMembers
            });
        }

        sortedThresholds.Sort(ComparePartyCapacityThresholds);
        return sortedThresholds;
    }

    private static int ComparePartyCapacityThresholds(
        NecromancerPartyCapacityThreshold left,
        NecromancerPartyCapacityThreshold right)
    {
        int levelComparison = left.Level.CompareTo(right.Level);
        if (levelComparison != 0)
            return levelComparison;

        return left.MaxPartyMembers.CompareTo(right.MaxPartyMembers);
    }
}
