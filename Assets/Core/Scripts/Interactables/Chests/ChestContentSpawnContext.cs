using UnityEngine;

public readonly struct ChestContentSpawnContext
{
    public ChestContentSpawnContext(
        RoomContext roomContext,
        RoomGrid grid,
        ChestState chestState,
        Transform chestTransform,
        Vector3 spawnPosition)
    {
        RoomContext = roomContext;
        Grid = grid;
        ChestState = chestState;
        ChestTransform = chestTransform;
        SpawnPosition = spawnPosition;
    }

    public RoomContext RoomContext { get; }
    public RoomGrid Grid { get; }
    public ChestState ChestState { get; }
    public Transform ChestTransform { get; }
    public Vector3 SpawnPosition { get; }
}
