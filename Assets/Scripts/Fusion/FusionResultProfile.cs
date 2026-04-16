using UnityEngine;

[System.Serializable]
public class FusionResultProfile
{
    [SerializeField] private Faction _dominantFaction;
    [SerializeField] private Faction _secondaryFaction;
    [SerializeField] private ScriptableObject _statsTemplate;
    [SerializeField] private Sprite _creatureSprite;

    public Faction DominantFaction => _dominantFaction;
    public Faction SecondaryFaction => _secondaryFaction;
    public ScriptableObject StatsTemplate => _statsTemplate;
    public Sprite CreatureSprite => _creatureSprite;

    public bool MatchesCombinations(Faction dominant, Faction secondary)
    {
        return _dominantFaction == dominant && _secondaryFaction == secondary;
    }
}
