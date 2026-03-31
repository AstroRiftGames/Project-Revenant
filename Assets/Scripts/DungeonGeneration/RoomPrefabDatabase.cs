using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Dungeon/Room Prefab Database")]
public class RoomPrefabDatabase : ScriptableObject
{
    [SerializeField] private List<RoomPrefabEntry> roomPrefabs;

    public RoomPrefabEntry GetRandom(RoomType type)
    {
        var candidates = roomPrefabs.Where(r => r.RoomType == type).ToList();

        float totalWeight = 0f;
        foreach (var c in candidates)
            totalWeight += c.Weight;

        float roll = Random.value * totalWeight;

        foreach (var c in candidates)
        {
            roll -= c.Weight;
            if (roll <= 0f)
                return c;
        }

        return candidates[0];
    }
}