using UnityEngine;

[System.Serializable]
public class RoomPrefabEntry
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector2Int size;
    [SerializeField] private float weight = 1f;

    public RoomType RoomType => roomType;
    public GameObject Prefab => prefab;
    public Vector2Int Size => size;
    public float Weight => weight;
}