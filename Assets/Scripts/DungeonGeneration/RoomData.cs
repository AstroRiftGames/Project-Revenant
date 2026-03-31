using System.Collections.Generic;
using UnityEngine;

public class RoomData
{
    public int RoomID { get; private set; }
    public RoomType RoomType { get; set; }
    public Vector2Int GridPosition { get; set; }
    public Vector2Int Size { get; set; }
    public List<int> ConnectedRooms { get; private set; }
    public GameObject RoomInstance { get; set; }
    public RectInt Bounds
    {
        get
        {
            return new RectInt(
                GridPosition.x - Size.x / 2,
                GridPosition.y - Size.y / 2,
                Size.x,
                Size.y
            );
        }
    }

    public RoomData(int id)
    {
        RoomID = id;
        ConnectedRooms = new List<int>();
    }
}