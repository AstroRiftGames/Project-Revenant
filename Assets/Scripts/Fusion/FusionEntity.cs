using System;
using UnityEngine;

public class FusionEntity
{
    public string      Id          { get; private set; }
    public UnitFaction UnitFaction { get; private set; }
    public UnitRole    Role        { get; private set; }
    public StatBlock   Stats       { get; private set; }
    public Sprite      Visual      { get; private set; }
    public bool        IsDestroyed { get; private set; }

    public FusionEntity(string id, UnitFaction faction, UnitRole role, StatBlock stats, Sprite visual)
    {
        Id          = id;
        UnitFaction = faction;
        Role        = role;
        Stats       = stats;
        Visual      = visual;
        IsDestroyed = false;
    }

    public void Destroy()
    {
        IsDestroyed = true;
    }
}
