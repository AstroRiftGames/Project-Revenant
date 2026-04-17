using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class NecromancerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _necromancerPrefab;
    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private List<UnitData> _startingPartyMembers = new();
    [SerializeField] private int _maxPartyMembers = 3;
    [SerializeField] private bool _showPartyDebug;

    private NecromancerParty _party;
    private RoomPartySpawner _partySpawner;
    private NecromancerPartyContext _partyContext;
    private SoulContext _soulContext;
    private SoulBank _soulBank;
    private PartyDefeatDetector _partyDefeatDetector;
    private PartyDefeatReturnHandler _partyDefeatResolver;
    private NecromancerRoomTransitioner _roomTransitioner;
    private PrefabDungeonGenerator _dungeonGenerator;

    private bool _isFirstLaunch = true;

    private void Awake()
    {
        EnsurePartySystems();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        PrefabDungeonGenerator.OnFloorGenerated += HandleFloorGenerated;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        PrefabDungeonGenerator.OnFloorGenerated -= HandleFloorGenerated;
    }

    private void Start()
    {
        if (_isFirstLaunch)
        {
            _isFirstLaunch = false;
            // First time play mode is pressed, manually trigger setup 
            // since SceneLoaded already passed before this component awakened.
            InitializeScene();
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!_isFirstLaunch)
        {
            InitializeScene();
        }
    }

    private void InitializeScene()
    {
        _floorManager = FindFirstObjectByType<FloorManager>();
        _dungeonGenerator = FindFirstObjectByType<PrefabDungeonGenerator>();
        EnsurePartySystems();

        if (_dungeonGenerator != null)
        {
            if (_dungeonGenerator.CurrentFloorData != null)
            {
                Spawn(); // Ya se generó por Start() antes de nosotros
            }
            // Si es null, esperamos a que el evento OnFloorGenerated avise que terminó.
        }
        else
        {
            // Sin generador estricto (Modo SafeZone).
            Spawn();
        }
    }

    private void HandleFloorGenerated(PDFloorData floorData)
    {
        // Solo spawneamos al jugador a causa de la generación del piso SI NO EXISTE un jugador todavía.
        // Es decir, al iniciar o recargar escena entera. NO al pasar de piso en la misma escena.
        if (FindFirstObjectByType<Necromancer>() == null)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        if (!ValidateSetup(out RoomContext roomContext, out RoomGrid grid))
            return;

        Vector3Int spawnCell = FindSpawnCell(grid, roomContext);
        Vector3 spawnPosition = grid.CellToWorld(spawnCell);

        GameObject instance = Instantiate(_necromancerPrefab, spawnPosition, Quaternion.identity);

        if (!instance.TryGetComponent(out Necromancer necromancer))
        {
            Debug.LogWarning("[NecromancerSpawner] El prefab del Nigromante no tiene componente Necromancer.", this);
            Destroy(instance);
            return;
        }

        necromancer.SetGrid(grid);
    }

    private void EnsurePartySystems()
    {
        _party = GetComponent<NecromancerParty>();
        if (_party == null)
            _party = gameObject.AddComponent<NecromancerParty>();

        _party.Configure(_startingPartyMembers, _maxPartyMembers, _showPartyDebug);

        _partySpawner = GetComponent<RoomPartySpawner>();
        if (_partySpawner == null)
            _partySpawner = gameObject.AddComponent<RoomPartySpawner>();

        _partySpawner.Configure(_floorManager, _party);

        _partyContext = GetComponent<NecromancerPartyContext>();
        if (_partyContext == null)
            _partyContext = gameObject.AddComponent<NecromancerPartyContext>();

        _soulContext = GetComponent<SoulContext>();
        if (_soulContext == null)
            _soulContext = gameObject.AddComponent<SoulContext>();

        _soulBank = GetComponent<SoulBank>();
        if (_soulBank == null)
            _soulBank = gameObject.AddComponent<SoulBank>();

        _partyContext.Configure(_party, _partySpawner);
        _soulContext.Configure(_soulBank);

        _partyDefeatDetector = GetComponent<PartyDefeatDetector>();
        if (_partyDefeatDetector == null)
            _partyDefeatDetector = gameObject.AddComponent<PartyDefeatDetector>();

        _partyDefeatDetector.Configure(_party, _floorManager);

        _partyDefeatResolver = GetComponent<PartyDefeatReturnHandler>();
        if (_partyDefeatResolver == null)
            _partyDefeatResolver = gameObject.AddComponent<PartyDefeatReturnHandler>();

        _roomTransitioner = GetComponent<NecromancerRoomTransitioner>();
        if (_roomTransitioner == null)
            _roomTransitioner = gameObject.AddComponent<NecromancerRoomTransitioner>();

        _dungeonGenerator = FindFirstObjectByType<PrefabDungeonGenerator>();

        _partyDefeatResolver.Configure(_partyDefeatDetector);
    }

    private Vector3Int FindSpawnCell(RoomGrid grid, RoomContext roomContext)
    {
        List<Vector3Int> availableCells = roomContext.GetAvailableSpawnCells();
        if (availableCells.Count > 0)
        {
            Vector3 roomOrigin = roomContext.transform.position;
            Vector3Int bestCell = availableCells[0];
            float bestDistance = Vector3.Distance(roomOrigin, grid.CellToWorld(bestCell));

            for (int i = 1; i < availableCells.Count; i++)
            {
                Vector3Int candidate = availableCells[i];
                float distance = Vector3.Distance(roomOrigin, grid.CellToWorld(candidate));
                if (distance >= bestDistance)
                    continue;

                bestCell = candidate;
                bestDistance = distance;
            }

            return bestCell;
        }

        Vector3Int centerCell = grid.WorldToCell(roomContext.transform.position);

        return grid.FindClosestWalkableCell(centerCell, centerCell);
    }

    private bool ValidateSetup(out RoomContext roomContext, out RoomGrid grid)
    {
        roomContext = null;
        grid = null;

        if (_necromancerPrefab == null)
        {
            Debug.LogWarning("[NecromancerSpawner] No hay prefab del Nigromante asignado.", this);
            return false;
        }

        if (_floorManager != null && _floorManager.CurrentRoom != null)
        {
            _floorManager.CurrentRoom.TryGetComponent(out roomContext);
        }
        else
        {
            // Modo SafeZone: Si no hay generador procedural, tomamos la primera sala estática que exista en el mapa.
            roomContext = FindFirstObjectByType<RoomContext>();
        }

        if (roomContext == null)
        {
            Debug.LogWarning("[NecromancerSpawner] No se encontro ni una sala procedural ni un RoomContext estático en la escena.", this);
            return false;
        }

        grid = roomContext.RoomGrid;
        if (grid == null)
        {
            Debug.LogWarning("[NecromancerSpawner] RoomContext no tiene BattleGrid resuelto. Asegurate de que la sala tenga tilemaps configurados.", this);
            return false;
        }

        return true;
    }
}
