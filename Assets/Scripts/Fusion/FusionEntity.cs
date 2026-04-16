using System;
using UnityEngine;

public class FusionEntity
{
    public string Id { get; private set; }
    public UnitFaction UnitFaction { get; private set; }
    public StatBlock Stats { get; private set; }
    public Sprite Visual { get; private set; }
    public bool IsDestroyed { get; private set; }

    public FusionEntity(string id, UnitFaction faction, StatBlock stats, Sprite visual)
    {
        Id = id;
        UnitFaction = faction;
        Stats = stats;
        Visual = visual;
        IsDestroyed = false;
    }

    public void Destroy()
    {
        IsDestroyed = true;
    }
}

