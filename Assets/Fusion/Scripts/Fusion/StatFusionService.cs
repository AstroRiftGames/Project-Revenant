using System.Collections.Generic;
using UnityEngine;

public class StatFusionService
{
    private readonly StatFusionConfig _config;

    public StatFusionService(StatFusionConfig config)
    {
        _config = config;
    }

    public StatBlock GenerateStats(FusionEntity a, FusionEntity b, UnitFaction dominantFaction)
    {
        StatBlock result = new StatBlock();
        HashSet<StatType> allStatIds = new HashSet<StatType>();

        foreach (StatEntry stat in a.Stats.Entries) allStatIds.Add(stat.StatId);
        foreach (StatEntry stat in b.Stats.Entries) allStatIds.Add(stat.StatId);

        FactionStatBonus bonusConfig = GetFactionBonusConfig(dominantFaction);

        foreach (StatType statId in allStatIds)
        {
            float valA = a.Stats.GetStat(statId);
            float valB = b.Stats.GetStat(statId);

            float baseAvg = (valA + valB) / 2f;

            if (bonusConfig != null && bonusConfig.BonusStats.Contains(statId))
            {
                baseAvg *= bonusConfig.BonusMultiplier;
            }

            float variance = Random.Range(_config.MinRandomVariance, _config.MaxRandomVariance);
            float finalValue = baseAvg * variance;
            
            result.AddOrUpdateStat(statId, finalValue);
        }

        return result;
    }

    private FactionStatBonus GetFactionBonusConfig(UnitFaction faction)
    {
        if (_config == null || _config.FactionBonuses == null) return null;
        
        foreach (FactionStatBonus bonus in _config.FactionBonuses)
        {
            if (bonus.UnitFaction == faction) return bonus;
        }
        return null;
    }
}

