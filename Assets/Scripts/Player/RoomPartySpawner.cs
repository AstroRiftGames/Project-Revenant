using System.Collections.Generic;
using System.Linq;
using PrefabDungeonGeneration;
using UnityEngine;

public class RoomPartySpawner : MonoBehaviour
{
    [System.Serializable]
    private sealed class TemporaryFormationPattern
    {
        [SerializeField] private List<Vector2Int> _slotOffsets = CreateDefaultOffsets();

        public IReadOnlyList<Vector2Int> SlotOffsets => _slotOffsets;

        public bool TryGetSlotOffset(int slotIndex, Vector3Int forwardDirection, Vector3Int lateralDirection, out Vector3Int offset)
        {
            offset = forwardDirection;

            if (slotIndex < 0 || _slotOffsets == null || slotIndex >= _slotOffsets.Count)
                return false;

            Vector2Int localOffset = _slotOffsets[slotIndex];
            offset = (lateralDirection * localOffset.x) + (forwardDirection * localOffset.y);
            return true;
        }

        public void EnsureDefaults()
        {
            if (_slotOffsets == null || _slotOffsets.Count == 0)
                _slotOffsets = CreateDefaultOffsets();
        }

        public void ResetToDefaults()
        {
            _slotOffsets = CreateDefaultOffsets();
        }

        private static List<Vector2Int> CreateDefaultOffsets()
        {
            return new List<Vector2Int>
            {
                new(0, 1),
                new(1, 1),
                new(-1, 1),
                new(0, 2),
                new(1, 2),
                new(-1, 2),
                new(0, 3)
            };
        }
    }

    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private NecromancerParty _party;
    [SerializeField] private TemporaryFormationPattern _temporaryFormation = new();

    private readonly List<GameObject> _spawnedPartyUnits = new();

    public void Configure(FloorManager floorManager, NecromancerParty party)
    {
        _floorManager = floorManager;
        _party = party;
    }

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void Start()
    {
        if (_floorManager != null && _floorManager.CurrentRoom != null)
            DeployToRoom(_floorManager.CurrentRoom);
    }

    private void OnValidate()
    {
        _temporaryFormation ??= new TemporaryFormationPattern();
        _temporaryFormation.EnsureDefaults();
    }

    public void ResetTemporaryFormationToDefault()
    {
        _temporaryFormation ??= new TemporaryFormationPattern();
        _temporaryFormation.ResetToDefaults();
    }

    private void HandleRoomEntered(RoomDoor door, GameObject newRoom)
    {
        DeployToRoom(door, newRoom);
    }

    public void DeployToRoom(GameObject roomObject)
    {
        DeployToRoom(null, roomObject);
    }

    public void DeployToRoom(RoomDoor enteredDoor, GameObject roomObject)
    {
        ClearCurrentDeployment();

        if (_party == null || roomObject == null || !roomObject.TryGetComponent(out RoomContext roomContext))
            return;

        if (!IsCombatRoom(roomObject))
            return;

        RoomGrid grid = roomContext.RoomGrid;
        if (grid == null)
            return;

        List<PartyMemberData> deployableMembers = _party.GetDeployableMembers().ToList();
        if (deployableMembers.Count == 0)
            return;

        Vector3Int necromancerCell = ResolveFormationAnchorCell(grid, roomContext, enteredDoor, out Vector3Int forwardDirection);
        Vector3Int lateralDirection = new(-forwardDirection.y, forwardDirection.x, 0);
        var reservedCells = new HashSet<Vector3Int> { necromancerCell };
        _temporaryFormation ??= new TemporaryFormationPattern();
        _temporaryFormation.EnsureDefaults();

        int spawnIndex = 0;
        foreach (PartyMemberData member in deployableMembers)
        {
            if (member == null || member.UnitDefinition == null || member.UnitDefinition.unitPrefab == null)
                continue;

            if (!TryResolveFormationCell(
                    grid,
                    necromancerCell,
                    forwardDirection,
                    lateralDirection,
                    spawnIndex,
                    _temporaryFormation,
                    reservedCells,
                    out Vector3Int spawnCell))
            {
                break;
            }

            Vector3 spawnPosition = grid.CellToWorld(spawnCell);
            GameObject instance = Instantiate(member.UnitDefinition.unitPrefab, spawnPosition, Quaternion.identity, roomContext.transform);

            if (!instance.TryGetComponent(out Unit unit))
            {
                Debug.LogWarning($"[RoomPartySpawner] '{member.UnitDefinition.name}' prefab does not contain Unit.", this);
                Destroy(instance);
                continue;
            }

            reservedCells.Add(spawnCell);

            PartyMemberLink link = instance.GetComponent<PartyMemberLink>();
            if (link == null)
                link = instance.AddComponent<PartyMemberLink>();

            link.Initialize(member.PartyMemberId, true);
            unit.SetAffiliation(member.RuntimeTeam, member.RuntimeFaction);
            instance.GetComponent<LifeController>()?.SetCurrentHealth(Mathf.Max(1, member.CurrentHealth));

            TrackDeployment(instance, member.PartyMemberId);
            spawnIndex++;
        }
    }

    private static Vector3Int ResolveFormationAnchorCell(
        RoomGrid grid,
        RoomContext roomContext,
        RoomDoor enteredDoor,
        out Vector3Int forwardDirection)
    {
        Vector3Int roomCenterCell = grid.WorldToCell(roomContext.transform.position);
        Vector3Int resolvedArrivalCell =
            RoomTransitionPlacementUtility.ResolveArrivalCell(grid, roomContext, enteredDoor, out forwardDirection);

        Necromancer necromancer = FindAnyObjectByType<Necromancer>();
        if (necromancer == null || !necromancer.TryGetGrid(out RoomGrid necromancerGrid) || !ReferenceEquals(necromancerGrid, grid))
            return resolvedArrivalCell;

        Vector3Int necromancerCell = grid.WorldToCell(necromancer.transform.position);
        if (!grid.HasCell(necromancerCell))
            return resolvedArrivalCell;

        if (enteredDoor == null)
            forwardDirection = RoomTransitionPlacementUtility.GetBestInwardDirection(necromancerCell, roomCenterCell);

        return necromancerCell;
    }

    private static bool TryResolveFormationCell(
        RoomGrid grid,
        Vector3Int anchorCell,
        Vector3Int forwardDirection,
        Vector3Int lateralDirection,
        int slotIndex,
        TemporaryFormationPattern formationPattern,
        HashSet<Vector3Int> reservedCells,
        out Vector3Int resultCell)
    {
        resultCell = anchorCell;

        if (formationPattern == null ||
            !formationPattern.TryGetSlotOffset(slotIndex, forwardDirection, lateralDirection, out Vector3Int slotOffset))
        {
            return false;
        }

        Vector3Int desiredCell = anchorCell + slotOffset;
        return TryFindValidFormationCell(grid, desiredCell, anchorCell, forwardDirection, reservedCells, out resultCell);
    }

    private static bool TryFindValidFormationCell(
        RoomGrid grid,
        Vector3Int desiredCell,
        Vector3Int anchorCell,
        Vector3Int forwardDirection,
        HashSet<Vector3Int> reservedCells,
        out Vector3Int resultCell)
    {
        resultCell = desiredCell;
        int bestScore = int.MaxValue;
        bool found = false;

        for (int radius = 0; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int candidateCell = desiredCell + new Vector3Int(x, y, 0);
                    if (!grid.IsCellEnterable(candidateCell) || reservedCells.Contains(candidateCell))
                        continue;

                    int forwardScore = Mathf.RoundToInt(Vector3.Dot((Vector3)(candidateCell - anchorCell), (Vector3)forwardDirection) * 100f);
                    if (forwardScore < 0)
                        continue;

                    int score =
                        (radius * 1000) +
                        (GridNavigationUtility.GetCellDistance(desiredCell, candidateCell) * 10) -
                        forwardScore;

                    if (found && score >= bestScore)
                        continue;

                    bestScore = score;
                    resultCell = candidateCell;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private void ClearCurrentDeployment()
    {
        if (_party != null)
            _party.ClearDeploymentFlags();

        for (int i = _spawnedPartyUnits.Count - 1; i >= 0; i--)
        {
            GameObject instance = _spawnedPartyUnits[i];
            if (instance != null)
                Destroy(instance);
        }

        _spawnedPartyUnits.Clear();
    }

    public void TrackDeployment(GameObject instance, string partyMemberId)
    {
        if (instance == null)
            return;

        if (!_spawnedPartyUnits.Contains(instance))
            _spawnedPartyUnits.Add(instance);

        _party?.MarkDeployed(partyMemberId, true);
    }

    private static bool IsCombatRoom(GameObject roomObject)
    {
        if (!roomObject.TryGetComponent(out RoomPrefabProfile profile) || profile == null)
            return false;

        return profile.RoomType == PDRoomType.Combat ||
               profile.RoomType == PDRoomType.MiniBoss ||
               profile.RoomType == PDRoomType.Boss;
    }
}
