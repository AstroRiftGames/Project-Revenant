using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PrefabDungeonGeneration;

namespace ProjectRevenant.UI
{
    public class MinimapController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("El RectTransform donde se van a instanciar los nodos y líneas")]
        public RectTransform MapContainer;
        public MinimapRoomNodeUI NodePrefab;
        [Tooltip("Un GameObject con una imagen que se estirará horizontalmente para hacer de línea")]
        public RectTransform LinePrefab; 
        [Tooltip("Raíz visual del minimapa para prender o apagar. Poner acá el parent superior.")]
        public GameObject MinimapRoot;
        [Tooltip("Base de datos de iconos para mostrar el tipo de habitación.")]
        public Data.GameIconDatabase IconDatabase;

        [Header("Settings")]
        public float NodeSize = 30f;
        public float NodeSpacing = 50f;
        
        private Dictionary<int, MinimapRoomNodeUI> _roomNodes = new Dictionary<int, MinimapRoomNodeUI>();
        private Dictionary<string, GameObject> _connections = new Dictionary<string, GameObject>();
        private HashSet<string> _exploredRooms = new HashSet<string>();
        private Dictionary<int, Vector2Int> _logicalGridPos = new Dictionary<int, Vector2Int>();
        private FloorManager _floorManager;
        private Vector2 _centeringOffset;
        private float _currentScale = 1f;
        private int _currentDisplayedFloor = -1;

        private void Awake()
        {
            _floorManager = FindFirstObjectByType<FloorManager>();
            
            if (MinimapRoot == null) MinimapRoot = gameObject;
        }

        private void OnEnable()
        {
            PrefabDungeonGenerator.OnFloorGenerated += HandleFloorGenerated;
            FloorManager.OnRoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            PrefabDungeonGenerator.OnFloorGenerated -= HandleFloorGenerated;
            FloorManager.OnRoomEntered -= HandleRoomEntered;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMinimap();
            }
        }

        public void ToggleMinimap()
        {
            if (MinimapRoot != null)
            {
                MinimapRoot.SetActive(!MinimapRoot.activeSelf);
            }
        }

        private void HandleFloorGenerated(PDFloorData floorData)
        {
            ClearMinimap();

            if (floorData == null || floorData.Rooms == null) return;

            _currentDisplayedFloor = floorData.FloorIndex;

            CalculateLogicalGrid(floorData);

            foreach (var roomNode in floorData.Rooms)
            {
                if (_logicalGridPos.ContainsKey(roomNode.ID))
                {
                    CreateNode(roomNode);
                }
            }

            foreach (var roomNode in floorData.Rooms)
            {
                if (roomNode.ParentNode != null && _logicalGridPos.ContainsKey(roomNode.ID) && _logicalGridPos.ContainsKey(roomNode.ParentNode.ID))
                {
                    CreateConnection(roomNode.ParentNode, roomNode);
                }
            }

            if (_floorManager != null && _floorManager.CurrentRoom != null)
            {
                bool belongsToCurrentFloor = false;
                if (_floorManager.CurrentRoom.transform.parent != null)
                {
                    string parentName = _floorManager.CurrentRoom.transform.parent.name;
                    if (parentName.StartsWith("Floor_") && int.TryParse(parentName.Substring(6), out int floorIndex))
                    {
                        belongsToCurrentFloor = (floorIndex == _currentDisplayedFloor);
                    }
                }

                if (belongsToCurrentFloor)
                {
                    UpdateCurrentRoomHighlight(_floorManager.CurrentRoom);
                }
            }
        }

        private void CalculateLogicalGrid(PDFloorData floorData)
        {
            _logicalGridPos.Clear();
            if (floorData.Rooms.Count == 0) return;

            PDRoomNode root = null;
            foreach (var r in floorData.Rooms)
            {
                if (r.ParentNode == null) 
                {
                    root = r;
                    break;
                }
            }

            if (root == null) root = floorData.Rooms[0];

            Queue<PDRoomNode> queue = new Queue<PDRoomNode>();
            queue.Enqueue(root);
            _logicalGridPos[root.ID] = Vector2Int.zero;

            Dictionary<int, List<PDRoomNode>> childrenMap = new Dictionary<int, List<PDRoomNode>>();
            foreach (var r in floorData.Rooms)
            {
                if (r.ParentNode != null)
                {
                    if (!childrenMap.ContainsKey(r.ParentNode.ID)) 
                        childrenMap[r.ParentNode.ID] = new List<PDRoomNode>();
                    childrenMap[r.ParentNode.ID].Add(r);
                }
            }

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
            occupied.Add(Vector2Int.zero);

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                Vector2Int currPos = _logicalGridPos[curr.ID];

                if (!childrenMap.ContainsKey(curr.ID)) continue;

                foreach (var child in childrenMap[curr.ID])
                {
                    Vector2 dirRaw = new Vector2(
                        (child.WorldPosition.x + child.Size.x/2f) - (curr.WorldPosition.x + curr.Size.x/2f),
                        (child.WorldPosition.y + child.Size.y/2f) - (curr.WorldPosition.y + curr.Size.y/2f)
                    );

                    Vector2Int dir = Vector2Int.zero;
                    if (Mathf.Abs(dirRaw.x) > Mathf.Abs(dirRaw.y))
                    {
                        dir = dirRaw.x > 0 ? Vector2Int.right : Vector2Int.left;
                    }
                    else
                    {
                        dir = dirRaw.y > 0 ? Vector2Int.up : Vector2Int.down;
                    }

                    if (dir == Vector2Int.zero) dir = Vector2Int.right;

                    Vector2Int desiredPos = currPos + dir;

                    if (occupied.Contains(desiredPos))
                    {
                        Vector2Int[] altDirs = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                        foreach(var alt in altDirs)
                        {
                            if (!occupied.Contains(currPos + alt))
                            {
                                desiredPos = currPos + alt;
                                break;
                            }
                        }
                    }

                    while (occupied.Contains(desiredPos))
                    {
                         desiredPos.x += 1; 
                    }

                    _logicalGridPos[child.ID] = desiredPos;
                    occupied.Add(desiredPos);
                    queue.Enqueue(child);
                }
            }

            if (_logicalGridPos.Count > 0)
            {
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach (var pos in _logicalGridPos.Values)
                {
                    if (pos.x < minX) minX = pos.x;
                    if (pos.x > maxX) maxX = pos.x;
                    if (pos.y < minY) minY = pos.y;
                    if (pos.y > maxY) maxY = pos.y;
                }

                _currentScale = 1f;
                if (MapContainer != null && MapContainer.rect.width > 0 && MapContainer.rect.height > 0)
                {
                    float widthNeeded = (maxX - minX) * NodeSpacing + NodeSize * 2f;
                    float heightNeeded = (maxY - minY) * NodeSpacing + NodeSize * 2f;
                    
                    if (widthNeeded > 0 && heightNeeded > 0)
                    {
                        float scaleX = MapContainer.rect.width / widthNeeded;
                        float scaleY = MapContainer.rect.height / heightNeeded;
                        _currentScale = Mathf.Min(scaleX, scaleY);
                    }
                }

                Vector2 centerLogical = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
                _centeringOffset = -centerLogical * NodeSpacing * _currentScale;
            }
            else
            {
                _centeringOffset = Vector2.zero;
                _currentScale = 1f;
            }
        }

        private void CreateNode(PDRoomNode roomNode)
        {
            if (NodePrefab == null || MapContainer == null) return;

            MinimapRoomNodeUI nodeUI = Instantiate(NodePrefab, MapContainer);
            nodeUI.gameObject.name = $"MinimapNode_{roomNode.ID}";
            
            RectTransform rt = nodeUI.GetComponent<RectTransform>();
            
            Vector2Int logicalPos = _logicalGridPos[roomNode.ID];
            
            rt.anchoredPosition = new Vector2(logicalPos.x * NodeSpacing * _currentScale, logicalPos.y * NodeSpacing * _currentScale) + _centeringOffset;
            rt.sizeDelta = new Vector2(NodeSize * _currentScale, NodeSize * _currentScale);
            
            nodeUI.Initialize(roomNode, IconDatabase);
            _roomNodes[roomNode.ID] = nodeUI;
        }

        private void CreateConnection(PDRoomNode parentRoom, PDRoomNode childRoom)
        {
            if (LinePrefab == null || MapContainer == null) return;

            RectTransform lineRT = Instantiate(LinePrefab, MapContainer);
            lineRT.gameObject.name = $"MinimapConnection_{parentRoom.ID}_to_{childRoom.ID}";
            lineRT.SetAsFirstSibling();

            Vector2Int pLogical = _logicalGridPos[parentRoom.ID];
            Vector2Int cLogical = _logicalGridPos[childRoom.ID];

            Vector2 startPos = new Vector2(pLogical.x * NodeSpacing * _currentScale, pLogical.y * NodeSpacing * _currentScale) + _centeringOffset;
            Vector2 endPos = new Vector2(cLogical.x * NodeSpacing * _currentScale, cLogical.y * NodeSpacing * _currentScale) + _centeringOffset;

            Vector2 dir = endPos - startPos;
            float distance = dir.magnitude;

            lineRT.anchoredPosition = startPos + (dir / 2f);
            lineRT.sizeDelta = new Vector2(distance, lineRT.sizeDelta.y);
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            lineRT.localRotation = Quaternion.Euler(0, 0, angle);

            _connections[$"{parentRoom.ID}_{childRoom.ID}"] = lineRT.gameObject;
        }

        private void HandleRoomEntered(RoomDoor door, GameObject nextRoom)
        {
            if (nextRoom != null && nextRoom.transform.parent != null)
            {
                string parentName = nextRoom.transform.parent.name;
                if (parentName.StartsWith("Floor_"))
                {
                    if (int.TryParse(parentName.Substring(6), out int floorIndex))
                    {
                        if (floorIndex != _currentDisplayedFloor)
                        {
                            var gen = FindFirstObjectByType<PrefabDungeonGenerator>();
                            if (gen != null && gen.FloorsCache.ContainsKey(floorIndex))
                            {
                                HandleFloorGenerated(gen.FloorsCache[floorIndex]);
                            }
                        }
                    }
                }
            }

            UpdateCurrentRoomHighlight(nextRoom);
        }

        public void UpdateCurrentRoomHighlight(GameObject currentRoomGO)
        {
            if (currentRoomGO == null) return;

            int parsedID = -1;
            string[] parts = currentRoomGO.name.Split('_');
            if (parts.Length > 0)
            {
                if (int.TryParse(parts[parts.Length - 1], out parsedID)) { }
            }

            if (parsedID != -1)
            {
                _exploredRooms.Add($"{_currentDisplayedFloor}_{parsedID}");
            }

            foreach (var kvp in _roomNodes)
            {
                int roomID = kvp.Key;
                bool isExplored = _exploredRooms.Contains($"{_currentDisplayedFloor}_{roomID}");
                kvp.Value.gameObject.SetActive(isExplored);
                kvp.Value.SetIsCurrentRoom(roomID == parsedID);
            }

            foreach (var kvp in _connections)
            {
                string[] connParts = kvp.Key.Split('_');
                if (connParts.Length == 2 && int.TryParse(connParts[0], out int pID) && int.TryParse(connParts[1], out int cID))
                {
                    bool pExplored = _exploredRooms.Contains($"{_currentDisplayedFloor}_{pID}");
                    bool cExplored = _exploredRooms.Contains($"{_currentDisplayedFloor}_{cID}");
                    kvp.Value.SetActive(pExplored && cExplored);
                }
            }
        }

        private void ClearMinimap()
        {
            if (MapContainer == null) return;
            
            foreach (Transform child in MapContainer)
            {
                Destroy(child.gameObject);
            }
            _roomNodes.Clear();
            _connections.Clear();
            _logicalGridPos.Clear();
        }
    }
}
