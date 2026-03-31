using UnityEngine;

public interface IUnit
{
    string Id { get; }
    UnitRole Role { get; }
    UnitFaction Faction { get; }
    Vector3 Position { get; }
    bool CanDetect(IUnit candidate);
}
