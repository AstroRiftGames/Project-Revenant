using UnityEngine;
using System.Collections.Generic;


public class FloorGenerator
{
    private int roomCount = 10;

    public FloorData Generate(int floorIndex, int seed)
    {
        var floor = new FloorData(floorIndex, seed);

        CreateRooms(floor);
        CreateMainPath(floor);
        CreateBranches(floor);
        AssignSpecialRooms(floor);

        return floor;
    }
    private void CreateRooms(FloorData floor)
    {
        for (int i = 0; i < roomCount; i++)
        {
            floor.Rooms.Add(new RoomData(i));
        }
    }
    private void CreateMainPath(FloorData floor)
    {
        int pathLength = Random.Range(4, 7);

        List<RoomData> path = new List<RoomData>();

        for (int i = 0; i < pathLength; i++)
        {
            path.Add(floor.Rooms[i]);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Connect(path[i], path[i + 1]);
        }

        floor.StartRoom = path[0];
        floor.BossRoom = path[path.Count - 1];
    }
    private void Connect(RoomData a, RoomData b)
    {
        if (!a.ConnectedRooms.Contains(b.RoomID))
            a.ConnectedRooms.Add(b.RoomID);

        if (!b.ConnectedRooms.Contains(a.RoomID))
            b.ConnectedRooms.Add(a.RoomID);
    }
    private void CreateBranches(FloorData floor)
    {
        List<RoomData> unused = new List<RoomData>();

        foreach (var room in floor.Rooms)
        {
            if (room.ConnectedRooms.Count == 0)
                unused.Add(room);
        }

        foreach (var room in unused)
        {
            RoomData target = floor.Rooms[Random.Range(0, floor.Rooms.Count)];

            Connect(room, target);
        }
    }
    private void AssignSpecialRooms(FloorData floor)
    {
        floor.StartRoom.RoomType = RoomType.Start;

        bool isBossFloor = floor.FloorIndex % 5 == 0;

        floor.BossRoom.RoomType = isBossFloor
            ? RoomType.Boss
            : RoomType.MiniBoss;

        foreach (var room in floor.Rooms)
        {
            if (room == floor.StartRoom || room == floor.BossRoom)
                continue;

            room.RoomType = GetRandomRoomType();
        }
    }
    private RoomType GetRandomRoomType()
    {
        float roll = Random.value;

        if (roll < 0.6f) return RoomType.Combat;
        if (roll < 0.8f) return RoomType.Loot;
        if (roll < 0.95f) return RoomType.Shop;

        return RoomType.Combat;
    }
}