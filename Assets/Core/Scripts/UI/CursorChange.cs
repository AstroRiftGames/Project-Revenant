using Data;
using UnityEngine;

public class CursorChange : MonoBehaviour
{
    [SerializeField] private GameIconDatabase _database;
    [SerializeField] private CursorType cursorType = CursorType.Default;

    private struct CursorInfo
    {
        public Texture2D texture;
        public Vector2 hotspot;
        public CursorInfo(Texture2D texture, Vector2 hotspot)
        {
            this.texture = texture;
            this.hotspot = hotspot;
        }
    }
    CursorInfo ChangedCursor = new CursorInfo();
    CursorInfo DefaultCursor = new CursorInfo();

    private void Awake()
    {   
        (ChangedCursor.texture, ChangedCursor.hotspot) = _database.GetCursorIcon(cursorType);
        (DefaultCursor.texture, DefaultCursor.hotspot) = _database.GetCursorIcon(CursorType.Default);
    }

    public void OnMouseEnter()
    {
        Cursor.SetCursor(ChangedCursor.texture, ChangedCursor.hotspot, CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        Cursor.SetCursor(DefaultCursor.texture, DefaultCursor.hotspot, CursorMode.Auto);
    }
}
