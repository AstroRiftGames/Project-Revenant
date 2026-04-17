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
    private bool _hasGeneratedContent;

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
        ResolveDependencies();
        ConfigureGrid();
        GenerateContentIfNeeded();
        CacheRoomComponents();
        InjectContextIntoRoomComponents();
        CacheUnits();
        InjectContextIntoUnits();
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

        var result = new List<Vector3Int>();
        if (_roomGrid == null || _walkableTilemap == null)
            return result;

        BoundsInt bounds = _walkableTilemap.cellBounds;
        bool hasWalkableTiles = false;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!_walkableTilemap.HasTile(cell))
                continue;

            hasWalkableTiles = true;
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        if (!hasWalkableTiles)
            return result;

        int padding = Mathf.Max(0, edgePadding);
        for (int x = minX + padding; x <= maxX - padding; x++)
        {
            for (int y = minY + padding; y <= maxY - padding; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!_walkableTilemap.HasTile(cell))
                    continue;

                if (!_roomGrid.IsCellWalkable(cell))
                    continue;

                result.Add(cell);
            }
        }

        return result;
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
        if (_hasGeneratedContent || _contentGenerator == null)
            return;

        if (_roomGrid == null)
        {
            Debug.LogWarning(
                $"[RoomContext] '{name}': no puede generar contenido porque BattleGrid es null.",
                this);
            return;
        }

        _contentGenerator.GenerateContent(this);
        _hasGeneratedContent = true;
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
        if (_roomProfile == null)
            return false;

        return _roomProfile.RoomType == PDRoomType.Combat ||
               _roomProfile.RoomType == PDRoomType.MiniBoss ||
               _roomProfile.RoomType == PDRoomType.Boss;
    }
}
