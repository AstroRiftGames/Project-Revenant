using System;
using System.Collections.Generic;

public enum StatType
{
    MaxHealth,
    AttackDamage,
    AttackCooldown,
    AttackRange,
    PreferredDistance,
    Accuracy,
    Evasion,
    MovementSpeed,
    VisionRange
}

[Serializable]
public struct StatEntry
{
    public StatType StatId;
    public float Value;
}

[Serializable]
public class StatBlock
{
    public List<StatEntry> Entries = new List<StatEntry>();

    public float GetStat(StatType statId)
    {
        foreach (StatEntry entry in Entries)
        {
            if (entry.StatId == statId) return entry.Value;
        }
        return 0f;
    }

    public void AddOrUpdateStat(StatType statId, float value)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].StatId == statId)
            {
                StatEntry entry = Entries[i];
                entry.Value = value;
                Entries[i] = entry;
                return;
            }
        }
        Entries.Add(new StatEntry { StatId = statId, Value = value });
    }
}

