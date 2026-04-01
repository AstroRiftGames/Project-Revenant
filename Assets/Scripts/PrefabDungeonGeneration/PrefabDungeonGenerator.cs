using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PrefabDungeonGeneration
{
    [System.Serializable]
    public class PrefabRoomDictionaryEntry
    {
        public PDRoomType Type;
        public List<RoomPrefabProfile> Templates = new List<RoomPrefabProfile>();
    }

    public class PrefabDungeonGenerator : MonoBehaviour
    {
        public int Seed = 42;
        public int FloorNumber = 1;
        public int MinRooms = 8;
        public int MaxRooms = 13;
        public bool UseRandomSeed = true;

        public float TileWorldSize = 64f;

        public List<PrefabRoomDictionaryEntry> RoomPrototypes;
        public List<RoomTypeRule> BalanceRules;

        private PDFloorData _currentFloor;
        private List<GameObject> _spawnedInstances = new List<GameObject>();

        [ContextMenu("Generate Prefab Dungeon")]
        public void GenerateDungeon()
        {
            if (UseRandomSeed)
            {
                Seed = Random.Range(int.MinValue, int.MaxValue);
            }

            ClearDungeon();

            if (RoomPrototypes == null || RoomPrototypes.Count == 0) return;

            Dictionary<PDRoomType, List<RoomPrefabProfile>> templates = new Dictionary<PDRoomType, List<RoomPrefabProfile>>();
            foreach (var entry in RoomPrototypes)
            {
                if (!templates.ContainsKey(entry.Type))
                {
                    templates[entry.Type] = new List<RoomPrefabProfile>();
                }
                templates[entry.Type].AddRange(entry.Templates.Where(t => t != null));
            }

            var builder = new PrefabGraphBuilder(Seed);
            _currentFloor = builder.GenerateFloor(FloorNumber, MinRooms, MaxRooms, templates, TileWorldSize, BalanceRules);

            SpawnDungeon();
        }

        private void SpawnDungeon()
        {
            if (_currentFloor == null || _currentFloor.Rooms == null) return;

            GridLayout generatorGrid = GetComponent<GridLayout>();
            var roomSpawnData = new List<(PDRoomNode room, Vector3 spawnPos)>();

            foreach (var roomNode in _currentFloor.Rooms)
            {
                if (roomNode.PrefabProfile == null) continue;

                GridLayout activeGrid = generatorGrid;
                if (activeGrid == null)
                {
                    activeGrid = roomNode.PrefabProfile.GetComponentInParent<GridLayout>();
                    if (activeGrid == null) activeGrid = roomNode.PrefabProfile.GetComponentInChildren<GridLayout>();
                }

                Vector3 spawnPos;
                if (activeGrid != null)
                {
                    spawnPos = activeGrid.CellToLocal(new Vector3Int(roomNode.WorldPosition.x, roomNode.WorldPosition.y, 0));
                    spawnPos = transform.TransformPoint(spawnPos);
                }
                else
                {
                    spawnPos = new Vector3(roomNode.WorldPosition.x, roomNode.WorldPosition.y, 0) * TileWorldSize;
                    spawnPos = transform.TransformPoint(spawnPos);
                }
                
                roomSpawnData.Add((roomNode, spawnPos));
            }

            roomSpawnData.Sort((a, b) => b.spawnPos.y.CompareTo(a.spawnPos.y));

            foreach (var data in roomSpawnData)
            {
                PDRoomNode roomNode = data.room;
                Vector3 spawnPos = data.spawnPos;

                GameObject instance = Instantiate(roomNode.PrefabProfile.gameObject, spawnPos, Quaternion.identity, transform);
                instance.name = $"{roomNode.RoomType}_Room_{roomNode.ID}";
                
                _spawnedInstances.Add(instance);
            }
        }

        private void ClearDungeon()
        {
            for (int i = _spawnedInstances.Count - 1; i >= 0; i--)
            {
                if (_spawnedInstances[i] != null)
                {
                    if (Application.isPlaying) Destroy(_spawnedInstances[i]);
                    else DestroyImmediate(_spawnedInstances[i]);
                }
            }
            _spawnedInstances.Clear();
            _currentFloor = null;
        }

        private void OnDrawGizmos()
        {
            if (_currentFloor == null || _currentFloor.Rooms == null) return;

            GridLayout generatorGrid = GetComponent<GridLayout>();

            foreach (var room in _currentFloor.Rooms)
            {
                GridLayout activeGrid = generatorGrid;
                if (activeGrid == null && room.PrefabProfile != null)
                {
                    activeGrid = room.PrefabProfile.GetComponentInParent<GridLayout>();
                    if (activeGrid == null) activeGrid = room.PrefabProfile.GetComponentInChildren<GridLayout>();
                }

                Gizmos.color = new Color(0, 1, 0, 0.3f);
                
                Vector3 centerWorld;
                Vector3 sizeWorld;

                if (activeGrid != null)
                {
                    Vector3 cellBottomLeft = activeGrid.CellToLocal(new Vector3Int(room.WorldPosition.x, room.WorldPosition.y, 0));
                    Vector3 cellTopRight = activeGrid.CellToLocal(new Vector3Int(room.WorldPosition.x + room.Size.x, room.WorldPosition.y + room.Size.y, 0));
                    centerWorld = (cellBottomLeft + cellTopRight) / 2f;
                    sizeWorld = new Vector3(Mathf.Abs(cellTopRight.x - cellBottomLeft.x), Mathf.Abs(cellTopRight.y - cellBottomLeft.y), 1f);
                }
                else
                {
                    centerWorld = new Vector3(room.WorldPosition.x + room.Size.x / 2f, room.WorldPosition.y + room.Size.y / 2f, 0) * TileWorldSize;
                    sizeWorld = new Vector3(room.Size.x, room.Size.y, 1f) * TileWorldSize;
                }
                
                centerWorld = transform.TransformPoint(centerWorld);

                Gizmos.DrawCube(centerWorld, sizeWorld);
                
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(centerWorld, sizeWorld);

                foreach (var door in room.GlobalDoors)
                {
                    Gizmos.color = door.IsUsed ? Color.red : Color.cyan;
                    
                    Vector3 doorPos;
                    if (activeGrid != null)
                    {
                        doorPos = activeGrid.CellToLocal(new Vector3Int(door.Position.x, door.Position.y, 0));
                    }
                    else
                    {
                        doorPos = new Vector3(door.Position.x + 0.5f, door.Position.y + 0.5f, 0) * TileWorldSize;
                    }
                    
                    doorPos = transform.TransformPoint(doorPos);
                    Gizmos.DrawSphere(doorPos, (activeGrid != null ? 1f : TileWorldSize) * 0.2f);
                }
            }
        }
    }
}
