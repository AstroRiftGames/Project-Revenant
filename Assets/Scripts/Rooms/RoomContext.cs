using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomContext : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private BattleGrid _battleGrid;

    [Header("Tilemaps locales")]
    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;

    private readonly List<Unit> _units = new();

    public BattleGrid BattleGrid => _battleGrid;
    public IReadOnlyList<Unit> Units => _units;

    private void Awake()
    {
        ResolveGrid();
        ResolveTilemaps();
    }

    private void OnEnable()
    {
        InitializeRoom();
    }

    public void InitializeRoom()
    {
        ResolveGrid();
        ResolveTilemaps();
        ConfigureGrid();
        CacheUnits();
        InjectContextIntoUnits();
    }

    public void RegisterUnit(Unit unit)
    {
        if (unit != null && !_units.Contains(unit))
            _units.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        _units.Remove(unit);
    }

    private void ResolveGrid()
    {
        if (_battleGrid != null)
            return;

        _battleGrid = GetComponentInChildren<BattleGrid>(includeInactive: true);
        if (_battleGrid != null)
            return;

        if (BattleGrid.Instance != null)
        {
            _battleGrid = BattleGrid.Instance;
            return;
        }

        Debug.LogWarning(
            $"[RoomContext] '{name}': no se encontró ningún BattleGrid en la sala ni en la escena.",
            this);
    }

    private void ResolveTilemaps()
    {
        if (_walkableTilemap != null && _blockedTilemap != null)
            return;

        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(includeInactive: true);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null)
                continue;

            string tilemapName = tilemap.gameObject.name;

            if (_walkableTilemap == null &&
                (tilemapName.Equals("FloorTilemap") || tilemapName.Equals("WalkableTilemap")))
            {
                _walkableTilemap = tilemap;
                continue;
            }

            if (_blockedTilemap == null &&
                (tilemapName.Equals("WallTilemap") || tilemapName.Equals("BlockedTilemap")))
            {
                _blockedTilemap = tilemap;
            }
        }

        if (_walkableTilemap == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no se pudo resolver el tilemap walkable. " +
                $"Esperaba algo como 'FloorTilemap' o 'WalkableTilemap'.",
                this);
        }

        if (_blockedTilemap == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no se pudo resolver el tilemap blocked. " +
                $"Esperaba algo como 'WallTilemap' o 'BlockedTilemap'.",
                this);
        }
    }

    private void ConfigureGrid()
    {
        if (_battleGrid == null)
            return;

        if (_walkableTilemap == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no puede configurar BattleGrid porque falta el tilemap walkable.",
                this);
            return;
        }

        _battleGrid.Configure(_walkableTilemap, _blockedTilemap);
    }

    private void CacheUnits()
    {
        _units.Clear();
        GetComponentsInChildren(includeInactive: true, _units);
    }

    private void InjectContextIntoUnits()
    {
        foreach (Unit unit in _units)
        {
            if (unit == null)
                continue;

            unit.GetComponent<UnitMovement>()?.SetGrid(_battleGrid);
            unit.GetComponent<GridInputMover>()?.SetGrid(_battleGrid);
            unit.AssignRoomContext(this);
        }
    }
}