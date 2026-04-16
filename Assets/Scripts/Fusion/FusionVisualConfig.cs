using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FactionVisualFallback
{
    public UnitFaction UnitFaction;
    public Sprite FallbackSprite;
}

[CreateAssetMenu(fileName = "NewFusionVisualConfig", menuName = "Fusion/Visual Config")]
public class FusionVisualConfig : ScriptableObject
{
    public List<FusionResultProfile> CombinationOverrides = new List<FusionResultProfile>();
    public List<FactionVisualFallback> DominantFallbacks = new List<FactionVisualFallback>();
}

