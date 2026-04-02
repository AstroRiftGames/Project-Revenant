using UnityEngine;
using System.Collections.Generic;
using System;

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
    public class RoomContentConfig
    {
        [Tooltip("Min and Max number of items to spawn.")]
        public Vector2Int SpawnCountRange = new Vector2Int(1, 4);
        public List<ContentCategory> Categories = new List<ContentCategory>();
    }

    [RequireComponent(typeof(RoomPrefabProfile))]
    public class RoomContentGenerator : MonoBehaviour
    {
        [Header("Grid Rules")]
        [Tooltip("Size of each logical tile for spawning objects.")]
        public float TileSize = 1f;

        [Tooltip("Use this to align the spawning grid correctly if your room's pivot isn't perfectly centered.")]
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
        private List<Vector2> _occupiedPositions = new List<Vector2>();
        private bool _hasGeneratedContent = false;

        private void Awake()
        {
            _roomProfile = GetComponent<RoomPrefabProfile>();
        }

        [ContextMenu("Generate Content")]
        private Vector3 GridToLocal(Vector2 gridPos)
        {
            GridLayout gridLayout = GetComponentInParent<GridLayout>();
            if (gridLayout == null) gridLayout = GetComponentInChildren<GridLayout>();

            if (gridLayout != null)
            {
                return gridLayout.CellToLocalInterpolated(new Vector3(gridPos.x, gridPos.y, 0));
            }
            else
            {
                return new Vector3(gridPos.x * TileSize, gridPos.y * TileSize, 0);
            }
        }

        public void GenerateContent()
        {
            if (_hasGeneratedContent) 
            {
                return;
            }
            _hasGeneratedContent = true;

            if (_roomProfile == null) _roomProfile = GetComponent<RoomPrefabProfile>();
            if (_roomProfile == null)
            {
                return;
            }

            if (UseSeed)
            {
                UnityEngine.Random.InitState(RandomSeed);
            }

            RoomContentConfig configToUse = null;

            switch (_roomProfile.RoomType)
            {
                case PDRoomType.Combat:
                case PDRoomType.MiniBoss:
                case PDRoomType.Boss:
                    configToUse = CombatRoomConfig;
                    break;
                case PDRoomType.Loot:
                    configToUse = LootRoomConfig;
                    break;
                case PDRoomType.Shop:
                case PDRoomType.Altar:
                case PDRoomType.Start:
                default:
                    return;
            }

            if (configToUse == null || configToUse.Categories == null || configToUse.Categories.Count == 0) 
            {
                return;
            }

            int spawnCount = UnityEngine.Random.Range(configToUse.SpawnCountRange.x, configToUse.SpawnCountRange.y + 1);
            
            List<Vector2> validPositions = GetValidPositions();

            for (int i = 0; i < spawnCount; i++)
            {
                if (validPositions.Count == 0)
                {
                    break;
                }

                ContentCategory category = GetRandomCategory(configToUse.Categories);
                if (category == null || category.Prefabs.Count == 0) continue;

                SpawnableItem item = GetRandomPrefab(category.Prefabs);
                if (item == null || item.Prefab == null) continue;

                int randomPosIndex = UnityEngine.Random.Range(0, validPositions.Count);
                Vector2 chosenLocalPosition = validPositions[randomPosIndex];
                
                Vector3 localOffset = GridToLocal(chosenLocalPosition);
                Vector3 spawnWorldPos = transform.position + localOffset;
                
                GameObject spawnedObj = Instantiate(item.Prefab, spawnWorldPos, Quaternion.identity, transform);
                
                _occupiedPositions.Add(chosenLocalPosition);

                validPositions = FilterOverlappingPositions(validPositions, chosenLocalPosition);
            }
        }

        private List<Vector2> GetValidPositions()
        {
            List<Vector2> positions = new List<Vector2>();
            
            float halfX = (_roomProfile.Size.x - 1) / 2f;
            float halfY = (_roomProfile.Size.y - 1) / 2f;

            int startX = EdgePadding;
            int endX = _roomProfile.Size.x - EdgePadding;
            int startY = EdgePadding;
            int endY = _roomProfile.Size.y - EdgePadding;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Vector2 logicalPos = new Vector2(-halfX + x, -halfY + y) + SpawnOffset;
                    positions.Add(logicalPos);
                }
            }

            return positions;
        }

        private List<Vector2> FilterOverlappingPositions(List<Vector2> currentPositions, Vector2 newOccupiedGridPos)
        {
            List<Vector2> filteredPositions = new List<Vector2>();
            float minSqrDistance = 1.1f * 1.1f;

            foreach (var pos in currentPositions)
            {
                float sqrDistance = (pos - newOccupiedGridPos).sqrMagnitude;
                if (sqrDistance >= minSqrDistance)
                {
                    filteredPositions.Add(pos);
                }
            }

            return filteredPositions;
        }

        private ContentCategory GetRandomCategory(List<ContentCategory> categories)
        {
            int totalWeight = 0;
            foreach (var cat in categories)
            {
                totalWeight += cat.Weight;
            }

            if (totalWeight <= 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var cat in categories)
            {
                currentWeight += cat.Weight;
                if (randomValue < currentWeight)
                {
                    return cat;
                }
            }

            return null;
        }

        private SpawnableItem GetRandomPrefab(List<SpawnableItem> prefabs)
        {
            int totalWeight = 0;
            foreach (var item in prefabs)
            {
                totalWeight += item.Weight;
            }

            if (totalWeight <= 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var item in prefabs)
            {
                currentWeight += item.Weight;
                if (randomValue < currentWeight)
                {
                    return item;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            if (_roomProfile == null)
            {
                _roomProfile = GetComponent<RoomPrefabProfile>();
            }

            if (_roomProfile == null) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            List<Vector2> validPositions = GetValidPositions();
            
            foreach (var pos in validPositions)
            {
                Vector3 worldPos = transform.position + GridToLocal(pos);
                Gizmos.DrawSphere(worldPos, 0.15f);
            }

            if (Application.isPlaying && _occupiedPositions.Count > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                foreach (var occ in _occupiedPositions)
                {
                    Vector3 worldPos = transform.position + GridToLocal(occ);
                    Gizmos.DrawSphere(worldPos, 0.3f);
                }
            }
        }
    }
}
