using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomContext : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private RoomGrid _roomGrid;
    [SerializeField] private RoomContentGenerator _contentGenerator;
    [SerializeField] private CombatRoomController _combatController;
    [SerializeField] private RoomPrefabProfile _roomProfile;

    [Header("Local tilemaps")]
    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;

    private readonly List<Unit> _units = new();
    private readonly List<MonoBehaviour> _roomComponents = new();
    private RoomDoor[] _doors;

    public RoomGrid RoomGrid => _roomGrid;
    public CombatRoomController CombatController => _combatController;
    public bool IsCombatRoom => ResolveIsCombatRoom();
    public IReadOnlyList<Unit> Units => _units;

    private void Awake()
    {
        ResolveDependencies();
    }

    public void EnterRoom()
    {
        // Re-enable this RoomContext and its gameplay components in case
        // they were disabled by a previous ExitRoom() call (room re-entry).
        enabled = true;

        ResolveDependencies();
        ConfigureGrid();
        GenerateContentIfNeeded();
        CacheRoomComponents();
        InjectContextIntoRoomComponents();
        CacheUnits();
        InjectContextIntoUnits();

        // Re-enable gameplay components that were disabled on exit.
        if (_combatController != null)
            _combatController.enabled = true;

        if (_contentGenerator != null)
            _contentGenerator.enabled = true;

        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] != null)
                _units[i].enabled = true;
        }

        // Activate all connected doors so they register grid occupancy and become interactable.
        // Doors that were intentionally deactivated during dungeon generation (unused doors
        // that don't connect to any room) are left inactive.
        CacheDoors();
        for (int i = 0; i < _doors.Length; i++)
        {
            RoomDoor door = _doors[i];
            if (door != null && door.roomA != null && door.roomB != null)
                door.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the player leaves this room. Disables gameplay components
    /// (RoomContext, CombatRoomController, units, doors, etc.) while keeping
    /// the room GameObject active so that tilemaps, sprites, and other visuals
    /// remain visible. This makes previously visited rooms stay rendered.
    /// </summary>
    public void ExitRoom()
    {
        // Disable this RoomContext so its Awake/Update/etc. no longer run.
        enabled = false;

        // Disable the combat controller if present.
        if (_combatController != null)
            _combatController.enabled = false;

        // Disable the content generator.
        if (_contentGenerator != null)
            _contentGenerator.enabled = false;

        // Disable all units in the room so they stop processing.
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] != null)
                _units[i].enabled = false;
        }

        // Deactivate all doors so they release grid occupancy and can be
        // properly re-initialized when the player returns to this room.
        CacheDoors();
        for (int i = 0; i < _doors.Length; i++)
        {
            if (_doors[i] != null)
                _doors[i].gameObject.SetActive(false);
        }
    }

    private void CacheDoors()
    {
        if (_doors != null && _doors.Length > 0)
            return;

        _doors = GetComponentsInChildren<RoomDoor>(includeInactive: true);
    }

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || _units.Contains(unit))
            return;

        _units.Add(unit);
        IntegrateUnit(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit != null && ReferenceEquals(unit.RoomContext, this))
            unit.AssignRoomContext(null);

        _units.Remove(unit);
    }

    public List<Vector3Int> GetAvailableSpawnCells(int edgePadding = 0)
    {
        ResolveDependencies();
        return RoomSpawnCellUtility.GetAvailableSpawnCells(_walkableTilemap, _roomGrid, edgePadding);
    }

    private void ResolveDependencies()
    {
        ResolveGrid();
        ResolveTilemaps();
        ResolveContentGenerator();
        ResolveRoomProfile();
        ResolveCombatController();
    }

    private void ResolveGrid()
    {
        if (_roomGrid != null)
            return;

        _roomGrid = GetComponentInChildren<RoomGrid>(includeInactive: true);
        if (_roomGrid == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no se encontro ningun BattleGrid dentro de la sala.",
                this);
        }
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

    private void ResolveContentGenerator()
    {
        if (_contentGenerator != null)
            return;

        _contentGenerator = GetComponentInChildren<RoomContentGenerator>(includeInactive: true);
    }

    private void ResolveRoomProfile()
    {
        if (_roomProfile != null)
            return;

        _roomProfile = GetComponent<RoomPrefabProfile>() ?? GetComponentInParent<RoomPrefabProfile>(includeInactive: true);
    }

    private void ResolveCombatController()
    {
        if (_combatController != null)
            return;

        _combatController = GetComponent<CombatRoomController>();

        if (_combatController == null)
            _combatController = GetComponentInChildren<CombatRoomController>(includeInactive: true);

        if (_combatController == null)
            _combatController = GetComponentInParent<CombatRoomController>(includeInactive: true);

        if (_combatController == null && IsCombatRoom)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': combat room without {nameof(CombatRoomController)}. Units in this room will be blocked.",
                this);
        }
    }

    private void ConfigureGrid()
    {
        if (_roomGrid == null)
            return;

        if (_walkableTilemap == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no puede configurar BattleGrid porque falta el tilemap walkable.",
                this);
            return;
        }

        _roomGrid.Configure(_walkableTilemap, _blockedTilemap);
    }

    private void GenerateContentIfNeeded()
    {
        if (_contentGenerator == null)
            return;

        if (_roomGrid == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no puede generar contenido porque BattleGrid es null.",
                this);
            return;
        }

        _contentGenerator.GenerateContent(this);
    }

    private void CacheUnits()
    {
        _units.Clear();
        GetComponentsInChildren(includeInactive: true, _units);
    }

    private void CacheRoomComponents()
    {
        _roomComponents.Clear();
        GetComponentsInChildren(includeInactive: true, _roomComponents);
    }

    private void InjectContextIntoRoomComponents()
    {
        for (int i = 0; i < _roomComponents.Count; i++)
        {
            MonoBehaviour component = _roomComponents[i];
            if (component is IRoomContextComponent roomContextComponent)
                roomContextComponent.IntegrateWithRoom(this);
        }
    }

    private void InjectContextIntoUnits()
    {
        if (_roomGrid == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': InjectContextIntoUnits abortado porque _battleGrid es null. " +
                $"Revisa que la sala tenga un BattleGrid hijo con tilemaps configurados.",
                this);
            return;
        }

        foreach (Unit unit in _units)
        {
            if (unit == null)
                continue;

            IntegrateUnit(unit);
        }
    }

    private void IntegrateUnit(Unit unit)
    {
        unit.IntegrateIntoRoom(this);
    }

    private bool ResolveIsCombatRoom()
    {
        return RoomCombatUtility.IsCombatRoom(_roomProfile);
    }
}