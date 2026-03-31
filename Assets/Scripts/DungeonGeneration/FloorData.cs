using System.Collections.Generic;

public class FloorData
{
    public int FloorIndex { get; private set; }
    public int Seed { get; private set; }

    public List<RoomData> Rooms { get; private set; }

    public RoomData StartRoom { get; set; }
    public RoomData BossRoom { get; set; }

    public FloorData(int floorIndex, int seed)
    {
        FloorIndex = floorIndex;
        Seed = seed;
        Rooms = new List<RoomData>();
    }
}