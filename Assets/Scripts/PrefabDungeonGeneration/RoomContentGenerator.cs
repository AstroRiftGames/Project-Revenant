using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabDungeonGeneration
{
    [Serializable]
    public class SpawnableItem
    {
        [Tooltip("The prefab to spawn.")]
        public GameObject Prefab;

        [Tooltip("Relative weight for random selection.")]
        [Min(1)]
        public int Weight = 1;
    }

    [Serializable]
    public class ContentCategory
    {
        public string CategoryName;

        [Tooltip("Relative weight for random selection amongst categories.")]
        [Min(1)]
        public int Weight = 1;

        public List<SpawnableItem> Prefabs = new List<SpawnableItem>();
    }

    [Serializable]
    public class FloorSpawnRange
    {
        [Tooltip("The minimum floor number required to use this range.")]
        public int MinFloor = 0;
        
        [Tooltip("Min and Max number of items to spawn.")]
        public Vector2Int SpawnCountRange = new Vector2Int(1, 4);
    }

    [Serializable]
    public class RoomContentConfig
    {
        [Tooltip("Legacy simple range (used if FloorSpawnRanges is empty).")]
        public Vector2Int SpawnCountRange = new Vector2Int(1, 4);
        
        [Tooltip("Ranges overrides based on floor thresholds. The highest MinFloor that is <= CurrentFloor is used.")]
        public List<FloorSpawnRange> FloorSpawnRanges = new List<FloorSpawnRange>();
        
        public List<ContentCategory> Categories = new List<ContentCategory>();

        public Vector2Int GetSpawnRangeForFloor(int floor)
        {
            if (FloorSpawnRanges == null || FloorSpawnRanges.Count == 0)
                return SpawnCountRange;

            Vector2Int bestRange = SpawnCountRange;
            int bestMatchedFloor = int.MinValue;

            for (int i = 0; i < FloorSpawnRanges.Count; i++)
            {
                var entry = FloorSpawnRanges[i];
                if (floor >= entry.MinFloor && entry.MinFloor > bestMatchedFloor)
                {
                    bestMatchedFloor = entry.MinFloor;
                    bestRange = entry.SpawnCountRange;
                }
            }
            
            return bestMatchedFloor == int.MinValue ? SpawnCountRange : bestRange;
        }
    }

    [RequireComponent(typeof(RoomPrefabProfile))]
    public class RoomContentGenerator : MonoBehaviour
    {
        [Header("Grid Rules")]
        [Tooltip("Legacy offset kept for compatibility. Valid spawn cells now come from RoomContext/BattleGrid.")]
        public Vector2 SpawnOffset = Vector2.zero;

        [Tooltip("Padding in tiles to avoid spawning too close to the walls. 1 means it leaves a 1-tile border empty.")]
        [Min(0)]
        public int EdgePadding = 0;

        [Header("Randomness")]
        public bool UseSeed = false;
        public int RandomSeed = 0;

        [Header("Room Content Rules")]
        public RoomContentConfig CombatRoomConfig;
        public RoomContentConfig LootRoomConfig;

        private RoomPrefabProfile _roomProfile;
        private readonly List<Vector3Int> _occupiedCells = new();
        private bool _hasGeneratedContent;

        private void Awake()
        {
            _roomProfile = GetComponent<RoomPrefabProfile>();
        }

        public void GenerateContent(RoomContext roomContext)
        {
            if (_hasGeneratedContent)
                return;

            if (_roomProfile == null)
                _roomProfile = GetComponent<RoomPrefabProfile>();
            if (_roomProfile == null)
                return;

            if (roomContext == null)
            {
                return;
            }

            RoomGrid grid = roomContext.RoomGrid;
            if (grid == null)
            {
                return;
            }

            if (UseSeed)
                UnityEngine.Random.InitState(RandomSeed);

            RoomContentConfig configToUse = GetConfigForRoomType();
            if (configToUse == null || configToUse.Categories == null || configToUse.Categories.Count == 0)
                return;

            List<Vector3Int> spawnCells = roomContext.GetAvailableSpawnCells(EdgePadding);
            if (spawnCells.Count == 0)
            {
                return;
            }

            _hasGeneratedContent = true;
            _occupiedCells.Clear();

            int currentFloor = 0;
            PrefabDungeonGenerator generator = FindObjectOfType<PrefabDungeonGenerator>();
            if (generator != null)
            {
                currentFloor = generator.FloorNumber;
            }

            Vector2Int floorRange = configToUse.GetSpawnRangeForFloor(currentFloor);
            int spawnCount = UnityEngine.Random.Range(floorRange.x, floorRange.y + 1);
            
            ShuffleList(spawnCells);

            int toSpawn = Mathf.Min(spawnCount, spawnCells.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                ContentCategory category = GetRandomCategory(configToUse.Categories);
                if (category == null || category.Prefabs.Count == 0)
                    continue;

                SpawnableItem item = GetRandomPrefab(category.Prefabs);
                if (item == null || item.Prefab == null)
                    continue;

                Vector3Int spawnCell = spawnCells[i];
                Instantiate(item.Prefab, grid.CellToWorld(spawnCell), Quaternion.identity, transform);
                _occupiedCells.Add(spawnCell);
            }
            Debug.Log($"Spawned {toSpawn} items in room {_roomProfile.RoomType}");
        }

        private RoomContentConfig GetConfigForRoomType()
        {
            switch (_roomProfile.RoomType)
            {
                case PDRoomType.Combat:
                case PDRoomType.MiniBoss:
                case PDRoomType.Boss:
                    return CombatRoomConfig;
                case PDRoomType.Loot:
                    return LootRoomConfig;
                case PDRoomType.Shop:
                case PDRoomType.Altar:
                case PDRoomType.Start:
                default:
                    return null;
            }
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private ContentCategory GetRandomCategory(List<ContentCategory> categories)
        {
            int totalWeight = 0;
            foreach (ContentCategory cat in categories)
            {
                totalWeight += cat.Weight;
            }

            if (totalWeight <= 0)
                return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (ContentCategory cat in categories)
            {
                currentWeight += cat.Weight;
                if (randomValue < currentWeight)
                    return cat;
            }

            return null;
        }

        private SpawnableItem GetRandomPrefab(List<SpawnableItem> prefabs)
        {
            int totalWeight = 0;
            foreach (SpawnableItem item in prefabs)
            {
                totalWeight += item.Weight;
            }

            if (totalWeight <= 0)
                return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (SpawnableItem item in prefabs)
            {
                currentWeight += item.Weight;
                if (randomValue < currentWeight)
                    return item;
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            RoomContext roomContext = GetComponent<RoomContext>();
            RoomGrid grid = roomContext != null ? roomContext.RoomGrid : null;
            if (roomContext == null || grid == null)
                return;

            List<Vector3Int> previewCells = roomContext.GetAvailableSpawnCells(EdgePadding);
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            for (int i = 0; i < previewCells.Count; i++)
            {
                Gizmos.DrawSphere(grid.CellToWorld(previewCells[i]), 0.15f);
            }

            if (Application.isPlaying && _occupiedCells.Count > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                for (int i = 0; i < _occupiedCells.Count; i++)
                {
                    Gizmos.DrawSphere(grid.CellToWorld(_occupiedCells[i]), 0.3f);
                }
            }
        }
    }
}
