using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Tilemaps;

public class DungeonManager : MonoBehaviour
{
    private DungeonData dungeonData;
    [SerializeField] private RoomPrefabDatabase roomDatabase;
    private FloorData currentFloor;
    [SerializeField] private Transform dungeonRoot;

    private FloorGenerator floorGenerator = new FloorGenerator();

    private float tileWidth = 1f;
    private float tileHeight = .5f;

    private void Start()
    {
        int seed = Random.Range(0, int.MaxValue);

        currentFloor = floorGenerator.Generate(1, seed);
        dungeonData = new DungeonData(seed);
        dungeonData.Floors.Add(currentFloor);

        StartCoroutine(BuildFloor(currentFloor));
    }
        
    private IEnumerator BuildFloor(FloorData floor)
    {
        GameObject floorGO = new GameObject($"Floor_{floor.FloorIndex}");
        floorGO.transform.SetParent(dungeonRoot);

        foreach (var room in floor.Rooms)
        {
            SpawnRoom(room, floorGO.transform);
            yield return new WaitForSeconds(1f);
        }

        StartCoroutine(SortFloor(currentFloor));
    }

    private void SpawnRoom(RoomData roomData, Transform parent)
    {
        var entry = roomDatabase.GetRandom(roomData.RoomType);

        roomData.Size = entry.Size;

        Vector2Int scaledGrid = new Vector2Int(
            roomData.GridPosition.x * roomData.Size.x,
            roomData.GridPosition.y * roomData.Size.y
        );

        Vector3 worldPos = GridToIsometric(scaledGrid);

        GameObject instance = Instantiate(entry.Prefab, worldPos, Quaternion.identity, parent);

        RoomController controller = instance.GetComponent<RoomController>();

        roomData.RoomInstance = instance;
        controller.Initialize(roomData);
    }

    Vector3 GridToIsometric(Vector2Int gridPos)
    {
        float x = (gridPos.x - gridPos.y) * (tileWidth / 2f);
        float y = (gridPos.x + gridPos.y) * (tileHeight / 2f);
        return new Vector3(x, y, 0f);
    }

    private IEnumerator SortFloor(FloorData floor)
    {
        List<RoomData> rooms = floor.Rooms;
        rooms.Sort((a, b) => b.GridPosition.y.CompareTo(a.GridPosition.y));
        rooms.Sort((a, b) => b.GridPosition.x.CompareTo(a.GridPosition.x));

        foreach (RoomData room in rooms)
        {
            RoomController controller = room.RoomInstance.GetComponent<RoomController>();
            TilemapRenderer renderer = null;
            if (controller != null)
            {
                renderer = controller.GetComponentInChildren<TilemapRenderer>();
            }

            if (renderer != null)
            {
                int sortingOrder = room.RoomID * -1;
                renderer.sortingOrder = sortingOrder;
            }
            yield return new WaitForSeconds(1f);
        }
    }
}