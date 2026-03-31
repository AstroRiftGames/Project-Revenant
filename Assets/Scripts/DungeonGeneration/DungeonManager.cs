using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    private DungeonData dungeonData;
    [SerializeField] private RoomPrefabDatabase roomDatabase;

    private void Awake()
    {
        int seed = Random.Range(0, int.MaxValue);
        dungeonData = new DungeonData(seed);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.H)) 
        {
            var room = roomDatabase.GetRandom(RoomType.Combat);
            Debug.Log(room.Prefab.name);
        }
    }
}