using UnityEngine;

[System.Serializable]
public class UnitStatsData
{
    public int maxHealth = 10;
    public int attackDamage = 1;
    public float attackInterval = 0.75f;
    public int attackRangeInCells = 1;
    public float moveSpeed = 2.5f;
    public float visionRange = 5f;
}

[CreateAssetMenu(fileName = "UnitData", menuName = "UnitData")]
public class UnitData : ScriptableObject
{
    public string unitId;
    public string displayName;
    public UnitRole role;
    public UnitFaction faction;
    public Sprite sprite;
    public int tileSize;
    public UnitStatsData stats = new();
}
