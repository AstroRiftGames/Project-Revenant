using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomController : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private Vector2Int size;
    [SerializeField] private RoomType roomType;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallsTilemap;

    [Header("Doors")]
    [SerializeField] private GameObject doorUp;
    [SerializeField] private GameObject doorDown;
    [SerializeField] private GameObject doorLeft;
    [SerializeField] private GameObject doorRight;

    public void Initialize(RoomData data)
    {
        gridPosition = data.GridPosition;
        size = data.Size;
        roomType = data.RoomType;
    }

    public void SetDoors(bool up, bool down, bool left, bool right)
    {
        if (doorUp != null) doorUp.SetActive(up);
        if (doorDown != null) doorDown.SetActive(down);
        if (doorLeft != null) doorLeft.SetActive(left);
        if (doorRight != null) doorRight.SetActive(right);
    }
    public void GenerateVisual(TileBase floorTile)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }
    }
}