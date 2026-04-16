using UnityEngine;

[System.Serializable]
public class FusionResultProfile
{
    [SerializeField] private UnitFaction _dominantFaction;
    [SerializeField] private UnitFaction _secondaryFaction;
    [SerializeField] private Sprite _creatureSprite;

    public UnitFaction DominantFaction => _dominantFaction;
    public UnitFaction SecondaryFaction => _secondaryFaction;
    public Sprite CreatureSprite => _creatureSprite;

    public bool MatchesCombinations(UnitFaction dominant, UnitFaction secondary)
    {
        return _dominantFaction == dominant && _secondaryFaction == secondary;
    }
}

