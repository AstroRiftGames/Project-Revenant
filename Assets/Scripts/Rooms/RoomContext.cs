using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomContext : MonoBehaviour
{
    [SerializeField] private BattleGrid _battleGrid;

    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;

    private readonly List<Unit> _units = new();

    public BattleGrid BattleGrid => _battleGrid;

    public IReadOnlyList<Unit> Units => _units;

    private void Awake()
    {
        ResolveGrid();
    }

    private void OnEnable()
    {
        InitializeRoom();
    }

    public void InitializeRoom()
    {
        ResolveGrid();
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

        Debug.LogWarning($"[RoomContext] '{gameObject.name}': no se encontró ningún BattleGrid. " +
                         "Agregá un BattleGrid en la jerarquía del prefab o asegurate de que exista uno en escena.", this);
    }

    private void ConfigureGrid()
    {
        if (_battleGrid == null)
            return;

        if (_walkableTilemap == null && _blockedTilemap == null)
            return;

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
            unit.GetComponent<UnitMovement>()?.SetGrid(_battleGrid);
            unit.GetComponent<GridInputMover>()?.SetGrid(_battleGrid);
            unit.AssignRoomContext(this);
        }
    }
}
