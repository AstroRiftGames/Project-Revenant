using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    private DungeonData dungeonData;

    private void Awake()
    {
        int seed = Random.Range(0, int.MaxValue);
        dungeonData = new DungeonData(seed);
    }
}