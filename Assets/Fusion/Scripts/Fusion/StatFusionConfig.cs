using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionStatBonus
{
    public UnitFaction UnitFaction;
    public List<StatType> BonusStats = new List<StatType>();
    public float BonusMultiplier = 1.25f;
}

[CreateAssetMenu(fileName = "NewStatFusionConfig", menuName = "Fusion/Stat Fusion Config")]
public class StatFusionConfig : ScriptableObject
{
    public float MinRandomVariance = 0.9f;
    public float MaxRandomVariance = 1.1f;
    public List<FactionStatBonus> FactionBonuses = new List<FactionStatBonus>();
}

