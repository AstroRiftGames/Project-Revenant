using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralDungeon
{
    public class DungeonGraphBuilder
    {
        private System.Random _rng;
        
        public DungeonGraphBuilder(int seed)
        {
            _rng = new System.Random(seed);
        }

        public List<ProceduralRoom> GenerateSpanningGraph(List<PRoomType> deck, Dictionary<PRoomType, RoomSetting> settings)
        {
            List<ProceduralRoom> rooms = new List<ProceduralRoom>();
            if (deck == null || deck.Count == 0 || settings == null) return rooms;

            PRoomType startType = deck[0];
            RoomSetting startSet = settings[startType];

            ProceduralRoom startRoom = new ProceduralRoom
            {
                ID = 0,
                RoomType = startType,
                Depth = 0,
                Bounds = new RectInt(0, 0, GetRandom(startSet.MinSize.x, startSet.MaxSize.x), GetRandom(startSet.MinSize.y, startSet.MaxSize.y))
            };
            rooms.Add(startRoom);

            int maxAttempts = 1000;

            for (int i = 1; i < deck.Count; i++)
            {
                PRoomType currentType = deck[i];
                RoomSetting currentSet = settings[currentType];
                bool isFinalNode = (i == deck.Count - 1);
                
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < maxAttempts)
                {
                    attempts++;
                    
                    ProceduralRoom sourceRoom = null;
                    if (isFinalNode)
                    {
                        int maxDepth = rooms.Max(r => r.Depth);
                        var candidates = rooms.Where(r => r.Depth == maxDepth).ToList();
                        sourceRoom = candidates[_rng.Next(0, candidates.Count)];
                    }
                    else
                    {
                        if (_rng.NextDouble() < 0.5f && rooms.Count > 1)
                        {
                            int idx = _rng.Next(Mathf.Max(0, rooms.Count - 3), rooms.Count);
                            sourceRoom = rooms[idx];
                        }
                        else
                        {
                            sourceRoom = rooms[_rng.Next(0, rooms.Count)];
                        }
                    }

                    DoorDirection dir = (DoorDirection)_rng.Next(0, 4);

                    if (sourceRoom.Doors.Any(d => d.Direction == dir)) continue;

                    Vector2Int newSize = new Vector2Int(GetRandom(currentSet.MinSize.x, currentSet.MaxSize.x), GetRandom(currentSet.MinSize.y, currentSet.MaxSize.y));
                    RectInt newBounds = GetBoundsForExpansion(sourceRoom.Bounds, newSize, dir);

                    bool hasOverlap = false;
                    foreach (var r in rooms)
                    {
                        if (r.Intersects(newBounds))
                        {
                            hasOverlap = true;
                            break;
                        }
                    }

                    if (!hasOverlap)
                    {
                        ProceduralRoom newRoom = new ProceduralRoom
                        {
                            ID = rooms.Count,
                            Bounds = newBounds,
                            RoomType = currentType,
                            Depth = sourceRoom.Depth + 1
                        };
                        
                        Vector2Int doorPos = DetermineDoorPosition(sourceRoom.Bounds, newBounds, dir);
                        sourceRoom.AddDoor(dir, doorPos, newRoom);
                        
                        DoorDirection opposite = GetOppositeDirection(dir);
                        newRoom.AddDoor(opposite, doorPos, sourceRoom);

                        rooms.Add(newRoom);
                        placed = true;
                    }
                }
            }

            return rooms;
        }

        private RectInt GetBoundsForExpansion(RectInt source, Vector2Int newSize, DoorDirection dir)
        {
            int xMin = 0, yMin = 0;
            switch (dir)
            {
                case DoorDirection.TopRight:
                    yMin = source.yMax;
                    xMin = _rng.Next(source.xMin - newSize.x + 1, source.xMax);
                    break;
                case DoorDirection.BottomRight:
                    xMin = source.xMax;
                    yMin = _rng.Next(source.yMin - newSize.y + 1, source.yMax);
                    break;
                case DoorDirection.BottomLeft:
                    yMin = source.yMin - newSize.y;
                    xMin = _rng.Next(source.xMin - newSize.x + 1, source.xMax);
                    break;
                case DoorDirection.TopLeft:
                    xMin = source.xMin - newSize.x;
                    yMin = _rng.Next(source.yMin - newSize.y + 1, source.yMax);
                    break;
            }
            return new RectInt(xMin, yMin, newSize.x, newSize.y);
        }

        private Vector2Int DetermineDoorPosition(RectInt source, RectInt newRoom, DoorDirection dir)
        {
            if (dir == DoorDirection.TopRight || dir == DoorDirection.BottomLeft)
            {
                int overlapMinX = Mathf.Max(source.xMin, newRoom.xMin);
                int overlapMaxX = Mathf.Min(source.xMax, newRoom.xMax);
                int midX = overlapMinX + (overlapMaxX - overlapMinX) / 2;
                return new Vector2Int(midX, (dir == DoorDirection.TopRight) ? source.yMax - 1 : source.yMin);
            }
            else
            {
                int overlapMinY = Mathf.Max(source.yMin, newRoom.yMin);
                int overlapMaxY = Mathf.Min(source.yMax, newRoom.yMax);
                int midY = overlapMinY + (overlapMaxY - overlapMinY) / 2;
                return new Vector2Int((dir == DoorDirection.BottomRight) ? source.xMax - 1 : source.xMin, midY);
            }
        }

        private DoorDirection GetOppositeDirection(DoorDirection dir)
        {
            switch (dir)
            {
                case DoorDirection.TopRight: return DoorDirection.BottomLeft;
                case DoorDirection.BottomRight: return DoorDirection.TopLeft;
                case DoorDirection.BottomLeft: return DoorDirection.TopRight;
                case DoorDirection.TopLeft: return DoorDirection.BottomRight;
                default: return DoorDirection.BottomLeft;
            }
        }

        private int GetRandom(int minInclusive, int maxInclusive)
        {
            return _rng.Next(minInclusive, maxInclusive + 1);
        }
    }
}
