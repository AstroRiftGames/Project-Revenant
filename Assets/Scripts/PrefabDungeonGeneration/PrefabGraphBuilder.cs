using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PrefabDungeonGeneration
{
    public class PrefabGraphBuilder
    {
        private System.Random _rng;

        public PrefabGraphBuilder(int seed)
        {
            _rng = new System.Random(seed);
        }

        public PDFloorData GenerateFloor(int floorIndex, int minRooms, int maxRooms, Dictionary<PDRoomType, List<RoomPrefabProfile>> templates, float tileSize, List<RoomTypeRule> rules)
        {
            PDFloorData bestFloor = null;
            int maxRoomsPlaced = 0;

            for (int globalAttempt = 0; globalAttempt < 100; globalAttempt++)
            {
                PDFloorData currentFloor = TryGenerateFloorOnce(floorIndex, minRooms, maxRooms, templates, tileSize, rules);
                
                if (currentFloor.Rooms.Count >= minRooms && currentFloor.Rooms.Any(r => r.RoomType == PDRoomType.Boss || r.RoomType == PDRoomType.MiniBoss))
                {
                    return currentFloor;
                }

                if (currentFloor.Rooms.Count > maxRoomsPlaced)
                {
                    maxRoomsPlaced = currentFloor.Rooms.Count;
                    bestFloor = currentFloor;
                }
            }

            return bestFloor ?? new PDFloorData { FloorIndex = floorIndex };
        }

        private PDFloorData TryGenerateFloorOnce(int floorIndex, int minRooms, int maxRooms, Dictionary<PDRoomType, List<RoomPrefabProfile>> templates, float tileSize, List<RoomTypeRule> rules)
        {
            PDFloorData floor = new PDFloorData { FloorIndex = floorIndex };
            
            if (!templates.ContainsKey(PDRoomType.Start) || templates[PDRoomType.Start].Count == 0) return floor;

            PDRoomType bossType = (floorIndex > 0 && floorIndex % 5 == 0) ? PDRoomType.Boss : PDRoomType.MiniBoss;
            if (!templates.ContainsKey(bossType) || templates[bossType].Count == 0) return floor;

            RoomPrefabProfile startTemplate = templates[PDRoomType.Start][_rng.Next(0, templates[PDRoomType.Start].Count)];
            PDRoomNode startNode = CreateNode(0, PDRoomType.Start, startTemplate, Vector2Int.zero, 0, tileSize);
            floor.Rooms.Add(startNode);

            bool bossPlaced = false;

            while (floor.Rooms.Count < maxRooms && !bossPlaced)
            {
                bool tryingBoss = floor.Rooms.Count >= (minRooms - 1);
                
                if (tryingBoss)
                {
                    bool bossSuccess = TryPlaceRoom(floor, bossType, templates[bossType], tileSize, true);
                    if (bossSuccess)
                    {
                        bossPlaced = true;
                        continue;
                    }
                }

                PDRoomType nextStandardType = GetRandomStandardType(floor, maxRooms, rules);
                if (!templates.ContainsKey(nextStandardType) || templates[nextStandardType].Count == 0) break;

                bool standardSuccess = TryPlaceRoom(floor, nextStandardType, templates[nextStandardType], tileSize, false);

                if (!standardSuccess)
                {
                    break;
                }
            }

            return floor;
        }

        private bool TryPlaceRoom(PDFloorData floor, PDRoomType type, List<RoomPrefabProfile> typeTemplates, float tileSize, bool forceMaxDepth)
        {
            int attempts = 0;
            while (attempts < 100)
            {
                attempts++;
                
                List<PDRoomNode> availableSources = floor.Rooms.Where(r => r.GlobalDoors.Any(d => !d.IsUsed)).ToList();
                if (availableSources.Count == 0) return false;

                List<PDRoomNode> validSources = availableSources;
                
                if (forceMaxDepth)
                {
                    int maxDepth = availableSources.Max(r => r.Depth);
                    validSources = availableSources.Where(r => r.Depth == maxDepth).ToList();
                }

                PDRoomNode sourceNode = validSources[_rng.Next(0, validSources.Count)];
                List<PDDoorAnchor> sourceOpenDoors = sourceNode.GlobalDoors.Where(d => !d.IsUsed).ToList();
                if (sourceOpenDoors.Count == 0) continue;

                PDDoorAnchor sourceDoor = sourceOpenDoors[_rng.Next(0, sourceOpenDoors.Count)];
                PDDoorDirection requiredDir = GetOppositeDirection(sourceDoor.Direction);

                RoomPrefabProfile chosenTemplate = typeTemplates[_rng.Next(0, typeTemplates.Count)];
                List<PDDoorAnchor> matchingLocalDoors = chosenTemplate.GetLogicalAnchors(tileSize).Where(d => d.Direction == requiredDir).ToList();
                if (matchingLocalDoors.Count == 0) continue;
                
                PDDoorAnchor chosenLocalDoor = matchingLocalDoors[_rng.Next(0, matchingLocalDoors.Count)];

                Vector2Int targetWorldPos = sourceDoor.Position - chosenLocalDoor.Position;
                RectInt newBounds = new RectInt(targetWorldPos.x, targetWorldPos.y, chosenTemplate.Size.x, chosenTemplate.Size.y);
                
                bool overlap = false;
                foreach (var room in floor.Rooms)
                {
                    if (room.Bounds.Overlaps(newBounds))
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    PDRoomNode newNode = CreateNode(floor.Rooms.Count, type, chosenTemplate, targetWorldPos, sourceNode.Depth + 1, tileSize);
                    newNode.ParentNode = sourceNode;
                    newNode.ParentDoorIndex = sourceDoor.OriginalIndex;
                    floor.Rooms.Add(newNode);
                    
                    sourceDoor.IsUsed = true;
                    foreach (var globalDoor in newNode.GlobalDoors)
                    {
                        if (globalDoor.Position == targetWorldPos + chosenLocalDoor.Position && globalDoor.Direction == chosenLocalDoor.Direction)
                        {
                            globalDoor.IsUsed = true;
                            break;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private PDRoomNode CreateNode(int id, PDRoomType type, RoomPrefabProfile template, Vector2Int worldPos, int depth, float tileSize)
        {
            PDRoomNode node = new PDRoomNode
            {
                ID = id,
                RoomType = type,
                PrefabProfile = template,
                WorldPosition = worldPos,
                Size = template.Size,
                Depth = depth
            };

            foreach (var localDoor in template.GetLogicalAnchors(tileSize))
            {
                node.GlobalDoors.Add(new PDDoorAnchor
                {
                    Position = worldPos + localDoor.Position,
                    Direction = localDoor.Direction,
                    IsUsed = localDoor.IsUsed,
                    OriginalIndex = localDoor.OriginalIndex
                });
            }

            return node;
        }

        private PDDoorDirection GetOppositeDirection(PDDoorDirection dir)
        {
            switch (dir)
            {
                case PDDoorDirection.TopRight: return PDDoorDirection.BottomLeft;
                case PDDoorDirection.BottomRight: return PDDoorDirection.TopLeft;
                case PDDoorDirection.BottomLeft: return PDDoorDirection.TopRight;
                case PDDoorDirection.TopLeft: return PDDoorDirection.BottomRight;
                default: return PDDoorDirection.TopRight;
            }
        }

        private PDRoomType GetRandomStandardType(PDFloorData floor, int maxRooms, List<RoomTypeRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                int[] weights = new int[] { 45, 20, 20, 15 };
                PDRoomType[] types = new PDRoomType[] { PDRoomType.Combat, PDRoomType.Loot, PDRoomType.Shop, PDRoomType.Altar };
                
                int rTotal = weights.Sum();
                int rRoll = _rng.Next(0, rTotal);
                int rCumulative = 0;

                for (int i = 0; i < weights.Length; i++)
                {
                    rCumulative += weights[i];
                    if (rRoll < rCumulative)
                    {
                        return types[i];
                    }
                }
                return PDRoomType.Combat;
            }

            List<RoomTypeRule> availableRules = new List<RoomTypeRule>(rules);

            for (int i = availableRules.Count - 1; i >= 0; i--)
            {
                RoomTypeRule rule = availableRules[i];
                if (rule.MaxCount >= 0)
                {
                    int currentCount = floor.Rooms.Count(r => r.RoomType == rule.Type);
                    if (currentCount >= rule.MaxCount)
                    {
                        availableRules.RemoveAt(i);
                    }
                }
            }

            int remainingSlots = maxRooms - floor.Rooms.Count;
            int slotsForStandard = remainingSlots - 1; 

            int requiredAdditions = 0;
            List<RoomTypeRule> deficitRules = new List<RoomTypeRule>();

            foreach (var rule in availableRules)
            {
                if (rule.MinCount > 0)
                {
                    int currentCount = floor.Rooms.Count(r => r.RoomType == rule.Type);
                    if (currentCount < rule.MinCount)
                    {
                        requiredAdditions += (rule.MinCount - currentCount);
                        deficitRules.Add(rule);
                    }
                }
            }

            if (requiredAdditions >= slotsForStandard && deficitRules.Count > 0)
            {
                availableRules = deficitRules;
            }

            if (availableRules.Count == 0)
            {
                return PDRoomType.Combat;
            }

            int totalWeight = availableRules.Sum(r => r.Weight);
            if (totalWeight <= 0) return availableRules[0].Type;

            int roll = _rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var rule in availableRules)
            {
                cumulative += rule.Weight;
                if (roll < cumulative)
                {
                    return rule.Type;
                }
            }

            return availableRules[0].Type;
        }
    }
}
