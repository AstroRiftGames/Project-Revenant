using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "UnitData")]
public class UnitData : ScriptableObject
{
    public string unitId;
    public string displayName;
    public UnitRole role;
    public UnitFaction faction;
    public Sprite sprite;
    public int tileSize;
    public int maxHealth = 10;
    public float visionRange;
    public float visionAngle;
}
