using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(NecromancerParty))]
[RequireComponent(typeof(RoomPartySpawner))]
[RequireComponent(typeof(NecromancerPartyContext))]
[RequireComponent(typeof(ManaContext))]
[RequireComponent(typeof(ManaBank))]
[RequireComponent(typeof(SoulContext))]
[RequireComponent(typeof(SoulBank))]
[RequireComponent(typeof(NecromancerProgressionContext))]
[RequireComponent(typeof(NecromancerProgressionBank))]
[RequireComponent(typeof(NecromancerManaCapacityProgressionAdapter))]
[RequireComponent(typeof(NecromancerPartyCapacityProgressionAdapter))]
[RequireComponent(typeof(NecromancerRoomExperienceTracker))]
[RequireComponent(typeof(PartyDefeatDetector))]
[RequireComponent(typeof(PartyDefeatReturnHandler))]
[RequireComponent(typeof(NecromancerRoomTransitioner))]
public class NecromancerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _necromancerPrefab;
    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private PrefabDungeonGenerator _dungeonGenerator;
    [SerializeField] private RoomContext _fallbackRoomContext;
    [SerializeField] private List<UnitData> _startingPartyMembers = new();
    [SerializeField] private int _maxPartyMembers = 3;
    [SerializeField] private bool _showPartyDebug;
    [SerializeField] private Necromancer _necromancer;

    private NecromancerParty _party;
    private RoomPartySpawner _partySpawner;
    private NecromancerPartyContext _partyContext;
    private ManaContext _manaContext;
    private ManaBank _manaBank;
    private SoulContext _soulContext;
    private SoulBank _soulBank;
    private NecromancerProgressionContext _progressionContext;
    private NecromancerManaCapacityProgressionAdapter _manaCapacityProgressionAdapter;
    private NecromancerPartyCapacityProgressionAdapter _partyCapacityProgressionAdapter;
    private NecromancerRoomExperienceTracker _roomExperienceTracker;
    private PartyDefeatDetector _partyDefeatDetector;
    private PartyDefeatReturnHandler _partyDefeatResolver;
    private NecromancerRoomTransitioner _roomTransitioner;
    private bool _isFirstLaunch = true;

    private void Awake()
    {
        if (!EnsurePartySystems())
            enabled = false;
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
            InitializeScene();
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!_isFirstLaunch)
            InitializeScene();
    }

    private void InitializeScene()
    {
        _floorManager = DungeonSceneReferenceUtility.ResolveFloorManager(_floorManager, this);
        _dungeonGenerator = DungeonSceneReferenceUtility.ResolveGenerator(_dungeonGenerator, this);
        _fallbackRoomContext = DungeonSceneReferenceUtility.ResolveRoomContext(_fallbackRoomContext, _floorManager, this);
        _necromancer = NecromancerReferenceUtility.Resolve(_necromancer, this);

        if (!EnsurePartySystems())
            return;

        _partyCapacityProgressionAdapter.Configure(_party, _progressionContext, _maxPartyMembers);
        _manaCapacityProgressionAdapter.Configure(_manaBank, _progressionContext, _manaBank != null ? _manaBank.MaximumMana : 0);
        _roomExperienceTracker.Configure(_progressionContext, _dungeonGenerator);

        if (_dungeonGenerator != null)
        {
            if (_dungeonGenerator.CurrentFloorData != null)
                Spawn();
        }
        else
        {
            Spawn();
        }
    }

    private void HandleFloorGenerated(PDFloorData floorData)
    {
        if (ResolveNecromancer() == null)
            Spawn();
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

        _necromancer = necromancer;
        _partySpawner.Configure(_floorManager, _party, _necromancer);
        _roomTransitioner.Configure(_necromancer);
        necromancer.SetGrid(grid);
    }

    private bool EnsurePartySystems()
    {
        _party = GetComponent<NecromancerParty>();
        _partySpawner = GetComponent<RoomPartySpawner>();
        _partyContext = GetComponent<NecromancerPartyContext>();
        _manaContext = GetComponent<ManaContext>();
        _manaBank = GetComponent<ManaBank>();
        _soulContext = GetComponent<SoulContext>();
        _soulBank = GetComponent<SoulBank>();
        _progressionContext = GetComponent<NecromancerProgressionContext>();
        _manaCapacityProgressionAdapter = GetComponent<NecromancerManaCapacityProgressionAdapter>();
        _partyCapacityProgressionAdapter = GetComponent<NecromancerPartyCapacityProgressionAdapter>();
        _roomExperienceTracker = GetComponent<NecromancerRoomExperienceTracker>();
        _partyDefeatDetector = GetComponent<PartyDefeatDetector>();
        _partyDefeatResolver = GetComponent<PartyDefeatReturnHandler>();
        _roomTransitioner = GetComponent<NecromancerRoomTransitioner>();

        if (_party == null ||
            _partySpawner == null ||
            _partyContext == null ||
            _manaContext == null ||
            _manaBank == null ||
            _soulContext == null ||
            _soulBank == null ||
            _progressionContext == null ||
            _manaCapacityProgressionAdapter == null ||
            _partyCapacityProgressionAdapter == null ||
            _roomExperienceTracker == null ||
            _partyDefeatDetector == null ||
            _partyDefeatResolver == null ||
            _roomTransitioner == null)
        {
            Debug.LogError(
                $"[{nameof(NecromancerSpawner)}] Manager prefab is missing one or more required components. " +
                "Add all party/progression scripts in authoring time instead of relying on runtime setup.",
                this);
            return false;
        }

        _party.Configure(_startingPartyMembers, _maxPartyMembers, _showPartyDebug);
        _partySpawner.Configure(_floorManager, _party, ResolveNecromancer());
        _partyContext.Configure(_party, _partySpawner);
        _manaContext.Configure(_manaBank);
        _soulContext.Configure(_soulBank);
        _progressionContext.Configure();
        _manaCapacityProgressionAdapter.Configure(_manaBank, _progressionContext, _manaBank.MaximumMana);
        _partyCapacityProgressionAdapter.Configure(_party, _progressionContext, _maxPartyMembers);
        _roomExperienceTracker.Configure(_progressionContext, _dungeonGenerator);
        _partyDefeatDetector.Configure(_party, _floorManager);
        _roomTransitioner.Configure(ResolveNecromancer());
        _partyDefeatResolver.Configure(_partyDefeatDetector);
        return true;
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
            roomContext = DungeonSceneReferenceUtility.ResolveRoomContext(_fallbackRoomContext, _floorManager, this);
        }

        if (roomContext == null)
        {
            Debug.LogWarning("[NecromancerSpawner] No se encontro ni una sala procedural ni un RoomContext estÃ¡tico en la escena.", this);
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

    private Necromancer ResolveNecromancer()
    {
        _necromancer = NecromancerReferenceUtility.Resolve(_necromancer, this);
        return _necromancer;
    }
}
