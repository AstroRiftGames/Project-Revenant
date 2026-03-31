using System.Collections.Generic;

public class DungeonData
{
    public int GlobalSeed { get; private set; }

    public List<FloorData> Floors { get; private set; }

    public DungeonData(int seed)
    {
        GlobalSeed = seed;
        Floors = new List<FloorData>();
    }
}