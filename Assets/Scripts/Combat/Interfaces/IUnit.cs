using UnityEngine;

public interface IUnit : IDamageable
{
    string Id { get; }
    UnitRole Role { get; }
    UnitFaction Faction { get; }
    Vector3 Position { get; }
    bool IscriaturaeTo(IUnit candidate);
    bool CanDetect(IUnit candidate);
}
