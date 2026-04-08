using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class UnitStatsData
{
    public int maxHealth = 10;
    public int attackDamage = 1;
    [FormerlySerializedAs("attackInterval")]
    public float attackCooldown = 0.75f;
    public int attackRangeInCells = 1;
    public int preferredDistanceInCells = 1;
    [Range(0f, 1f)] public float accuracy = 0.85f;
    [Range(0f, 1f)] public float evasion = 0.1f;
    public float moveSpeed = 2.5f;
    public float visionRange = 5f;
}

public enum UnitTeam
{
    Enemy,
    NecromancerAlly
}

public enum UnitCombatStyle
{
    Default,
    Melee,
    Ranged
}

public enum UnitTargetingMode
{
    RolePriority,
    Dynamic
}

[CreateAssetMenu(fileName = "UnitData", menuName = "UnitData")]
public class UnitData : ScriptableObject
{
    public string unitId;
    public string displayName;
    public GameObject unitPrefab;
    public UnitTeam team;
    public UnitRole role;
    public UnitCombatStyle combatStyle;
    public UnitTargetingMode targetingMode;
    public UnitFaction faction;
    public Sprite sprite;
    public int tileSize;
    public UnitStatsData stats = new();
}
