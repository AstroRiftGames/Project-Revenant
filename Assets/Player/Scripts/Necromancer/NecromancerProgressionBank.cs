using System;
using UnityEngine;

public readonly struct NecromancerProgressionSnapshot
{
    public NecromancerProgressionSnapshot(
        int currentLevel,
        int totalExperience,
        int experienceIntoCurrentLevel,
        int experienceRequiredForNextLevel,
        bool hasReachedMaxLevel)
    {
        CurrentLevel = currentLevel;
        TotalExperience = totalExperience;
        ExperienceIntoCurrentLevel = experienceIntoCurrentLevel;
        ExperienceRequiredForNextLevel = experienceRequiredForNextLevel;
        HasReachedMaxLevel = hasReachedMaxLevel;
    }

    public int CurrentLevel { get; }
    public int TotalExperience { get; }
    public int ExperienceIntoCurrentLevel { get; }
    public int ExperienceRequiredForNextLevel { get; }
    public bool HasReachedMaxLevel { get; }
}

[DisallowMultipleComponent]
public class NecromancerProgressionBank : MonoBehaviour
{
    [SerializeField, Min(1)] private int _currentLevel = 1;
    [SerializeField, Min(0)] private int _totalExperience;
    [SerializeField] private bool _hasInitialized;

    public int CurrentLevel => _currentLevel;
    public int TotalExperience => _totalExperience;

    public event Action<NecromancerProgressionSnapshot, int> OnProgressionChanged;
    public event Action<int, int> OnLevelChanged;
    public event Action<int> OnXPGained;

    public void Initialize(NecromancerProgressionProfile profile)
    {
        if (profile == null)
            return;

        int maximumTotalExperience = profile.GetMaximumTotalExperience();
        if (!_hasInitialized)
        {
            _currentLevel = Mathf.Max(1, profile.StartingLevel);
            _totalExperience = Mathf.Clamp(_totalExperience, profile.StartingTotalExperience, maximumTotalExperience);
            _hasInitialized = true;
        }
        else
        {
            _totalExperience = Mathf.Clamp(_totalExperience, profile.StartingTotalExperience, maximumTotalExperience);
        }

        _currentLevel = Mathf.Max(profile.StartingLevel, profile.GetLevelForExperience(_totalExperience));
        NotifyProgressionChanged(profile, 0);
    }

    public int AwardExperience(int amount, NecromancerProgressionProfile profile)
    {
        if (profile == null || amount <= 0)
        {
            return 0;
        }

        if (!_hasInitialized)
            Initialize(profile);

        int maximumTotalExperience = profile.GetMaximumTotalExperience();
        if (_totalExperience >= maximumTotalExperience)
        {
            _totalExperience = maximumTotalExperience;
            _currentLevel = Mathf.Max(profile.StartingLevel, profile.GetLevelForExperience(_totalExperience));
            NotifyProgressionChanged(profile, 0);
            return 0;
        }

        int previousLevel = _currentLevel;
        int awardedExperience = Mathf.Clamp(amount, 0, maximumTotalExperience - _totalExperience);
        _totalExperience += awardedExperience;
        _currentLevel = Mathf.Max(profile.StartingLevel, profile.GetLevelForExperience(_totalExperience));

        if (_currentLevel != previousLevel)
            OnLevelChanged?.Invoke(previousLevel, _currentLevel);

        if (awardedExperience > 0)
        {
            OnXPGained?.Invoke(awardedExperience);
        }

        NotifyProgressionChanged(profile, awardedExperience);
        return awardedExperience;
    }

    public NecromancerProgressionSnapshot GetSnapshot(NecromancerProgressionProfile profile)
    {
        if (profile == null)
        {
            return new NecromancerProgressionSnapshot(
                Mathf.Max(1, _currentLevel),
                Mathf.Max(0, _totalExperience),
                0,
                0,
                true);
        }

        int resolvedLevel = Mathf.Max(profile.StartingLevel, _currentLevel);
        int currentLevelStart = profile.GetRequiredTotalExperienceForLevel(resolvedLevel);
        int safeTotalExperience = Mathf.Max(0, _totalExperience);
        int experienceIntoCurrentLevel = Mathf.Max(0, safeTotalExperience - currentLevelStart);

        if (!profile.TryGetNextLevel(resolvedLevel, out _, out int requiredTotalExperienceForNextLevel))
        {
            return new NecromancerProgressionSnapshot(
                resolvedLevel,
                safeTotalExperience,
                experienceIntoCurrentLevel,
                0,
                true);
        }

        return new NecromancerProgressionSnapshot(
            resolvedLevel,
            safeTotalExperience,
            experienceIntoCurrentLevel,
            Mathf.Max(1, requiredTotalExperienceForNextLevel - currentLevelStart),
            false);
    }

    private void NotifyProgressionChanged(NecromancerProgressionProfile profile, int delta)
    {
        OnProgressionChanged?.Invoke(GetSnapshot(profile), delta);
    }
}
