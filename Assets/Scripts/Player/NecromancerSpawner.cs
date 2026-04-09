using System.Collections.Generic;
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

    private void Awake()
    {
        EnsurePartySystems();
    }

    private void Start()
    {
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

        if (_floorManager == null)
        {
            Debug.LogWarning("[NecromancerSpawner] No hay FloorManager asignado.", this);
            return false;
        }

        GameObject startRoom = _floorManager.CurrentRoom;
        if (startRoom == null)
        {
            Debug.LogWarning("[NecromancerSpawner] FloorManager no tiene sala inicial. " +
                             "Asegurate de que PrefabDungeonGenerator se ejecute antes (orden 0).", this);
            return false;
        }

        if (!startRoom.TryGetComponent(out roomContext))
        {
            Debug.LogWarning($"[NecromancerSpawner] La sala inicial '{startRoom.name}' no tiene RoomContext.", this);
            return false;
        }

        grid = roomContext.BattleGrid;
        if (grid == null)
        {
            Debug.LogWarning("[NecromancerSpawner] RoomContext no tiene BattleGrid resuelto. " +
                             "Asegurate de que la sala tenga tilemaps configurados.", this);
            return false;
        }

        return true;
    }
}
