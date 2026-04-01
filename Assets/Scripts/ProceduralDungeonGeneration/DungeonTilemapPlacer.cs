using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralDungeon
{
    public class DungeonTilemapPlacer : MonoBehaviour
    {
        public Tilemap FloorTilemap;
        public Tilemap WallTilemap;
        public TileBase DefaultDoorTile;
        public IsometricWallFader WallFader;
        
        public void BuildDungeon(IReadOnlyList<ProceduralRoom> rooms, Dictionary<PRoomType, RoomSetting> settingsDict)
        {
            if (FloorTilemap == null || WallTilemap == null) return;

            FloorTilemap.ClearAllTiles();
            WallTilemap.ClearAllTiles();
            
            if (WallFader != null)
            {
                WallFader.ClearWalls();
            }

            foreach (var room in rooms)
            {
                RoomSetting setting = settingsDict[room.RoomType];
                TileBase floorTile = setting.FloorTile;
                TileBase wallTile = setting.WallTile;

                for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
                {
                    for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
                    {
                        bool isSW = x == room.Bounds.xMin;
                        bool isSE = y == room.Bounds.yMin;
                        bool isNW = y == room.Bounds.yMax - 1;
                        bool isNE = x == room.Bounds.xMax - 1;

                        bool isColEdge = isSW || isNE;
                        bool isRowEdge = isSE || isNW;

                        if (isColEdge || isRowEdge)
                        {
                            Vector3Int pos0 = new Vector3Int(x, y, 0);
                            Vector3Int pos1 = new Vector3Int(x, y, 1);
                            
                            WallTilemap.SetTile(pos0, wallTile);
                            WallTilemap.SetTile(pos1, wallTile);

                            if (WallFader != null && (isSW || isSE))
                            {
                                WallFader.RegisterFrontWall(pos0);
                                WallFader.RegisterFrontWall(pos1);
                            }
                        }
                        else
                        {
                            FloorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                        }
                    }
                }

                foreach (var door in room.Doors)
                {
                    Vector3Int d0 = new Vector3Int(door.Position.x, door.Position.y, 0);
                    Vector3Int d1 = new Vector3Int(door.Position.x, door.Position.y, 1);
                    
                    WallTilemap.SetTile(d0, null); 
                    WallTilemap.SetTile(d1, null); 
                    
                    FloorTilemap.SetTile(d0, DefaultDoorTile != null ? DefaultDoorTile : floorTile);
                    
                    if (WallFader != null)
                    {
                        WallFader.UnregisterFrontWall(d0);
                        WallFader.UnregisterFrontWall(d1);
                    }
                }
            }
        }
    }
}
