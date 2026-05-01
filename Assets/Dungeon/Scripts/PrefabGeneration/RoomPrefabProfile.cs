using System.Collections.Generic;
using UnityEngine;

namespace PrefabDungeonGeneration
{
    [System.Serializable]
    public class PDDoorConfig
    {
        public Transform AnchorTransform;
        public PDDoorDirection Direction;
    }

    public class RoomPrefabProfile : MonoBehaviour
    {
        public PDRoomType RoomType;
        public Vector2Int Size = new Vector2Int(5, 5);
        public List<PDDoorConfig> Doors = new List<PDDoorConfig>();

        public List<PDDoorAnchor> GetLogicalAnchors(float tileSize)
        {
            List<PDDoorAnchor> anchors = new List<PDDoorAnchor>();
            
            GridLayout gridLayout = GetComponentInParent<GridLayout>();
            if (gridLayout == null) gridLayout = GetComponentInChildren<GridLayout>();

            for (int i = 0; i < Doors.Count; i++)
            {
                var d = Doors[i];
                if (d.AnchorTransform == null) continue;
                
                Vector3 relativePos = d.AnchorTransform.position - transform.position;
                Vector2Int pos;
                
                if (gridLayout != null)
                {
                    Vector3Int cell = gridLayout.LocalToCell(relativePos);
                    pos = new Vector2Int(cell.x, cell.y);
                }
                else
                {
                    pos = new Vector2Int(
                        Mathf.RoundToInt(relativePos.x / tileSize),
                        Mathf.RoundToInt(relativePos.y / tileSize)
                    );
                }

                anchors.Add(new PDDoorAnchor
                {
                    Position = pos,
                    Direction = d.Direction,
                    IsUsed = false,
                    OriginalIndex = i
                });
            }
            return anchors;
        }
    }
}
