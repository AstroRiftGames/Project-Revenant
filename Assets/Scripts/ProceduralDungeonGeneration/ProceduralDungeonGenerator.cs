using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralDungeon
{
    public class ProceduralDungeonGenerator : MonoBehaviour
    {
        public bool UseRandomSeed = true;
        public int Seed = 42;
        public int FloorNumber = 1;
        public int RoomCount = 10;
        
        public List<RoomSetting> RoomConfigs;

        public bool DrawGizmos = true;
        public float TileWorldSize = 64f; 

        private List<ProceduralRoom> _generatedRooms;

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon()
        {
            if (UseRandomSeed)
            {
                Seed = Random.Range(int.MinValue, int.MaxValue);
            }

            if (RoomConfigs == null || RoomConfigs.Count == 0)
            {
                return;
            }

            Dictionary<PRoomType, RoomSetting> settingsDict = RoomConfigs.ToDictionary(r => r.RoomType, r => r);

            var typeAssigner = new RoomTypeAssigner(Seed);
            List<PRoomType> deck = typeAssigner.BuildRoomDeck(RoomCount, FloorNumber);

            var graphBuilder = new DungeonGraphBuilder(Seed);
            _generatedRooms = graphBuilder.GenerateSpanningGraph(deck, settingsDict);

            if (_generatedRooms.Count > 0)
            {
                var placer = GetComponent<DungeonTilemapPlacer>();
                if (placer != null)
                {
                    placer.BuildDungeon(_generatedRooms, settingsDict);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!DrawGizmos || _generatedRooms == null) return;

            foreach (var room in _generatedRooms)
            {
                Color roomColor = GetColorForRoomType(room.RoomType);
                Gizmos.color = new Color(roomColor.r, roomColor.g, roomColor.b, 0.5f);

                Vector2 center = new Vector2(room.Bounds.center.x, room.Bounds.center.y) * TileWorldSize;
                Vector2 size = new Vector2(room.Bounds.width, room.Bounds.height) * TileWorldSize;
                
                Gizmos.DrawCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 1f));
                
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 1f));

                Gizmos.color = Color.cyan;
                foreach (var door in room.Doors)
                {
                    Vector2 doorWorldPos = new Vector2(door.Position.x + 0.5f, door.Position.y + 0.5f) * TileWorldSize;
                    Gizmos.DrawSphere(new Vector3(doorWorldPos.x, doorWorldPos.y, 0), TileWorldSize * 0.2f);
                }
            }
        }

        private Color GetColorForRoomType(PRoomType type)
        {
            switch (type)
            {
                case PRoomType.Start: return Color.green;
                case PRoomType.Combat: return Color.red;
                case PRoomType.Loot: return Color.yellow;
                case PRoomType.Shop: return new Color(1f, 0.5f, 0f);
                case PRoomType.Altar: return Color.magenta;
                case PRoomType.MiniBoss: return new Color(0.5f, 0f, 0.5f);
                case PRoomType.Boss: return Color.black;
                default: return Color.gray;
            }
        }
    }
}
