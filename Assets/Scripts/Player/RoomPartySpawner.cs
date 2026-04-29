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

            if (slotIndex < 0)
                return false;

            EnsureSlotOffsets(slotIndex + 1);
            if (_slotOffsets == null || slotIndex >= _slotOffsets.Count)
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

        private void EnsureSlotOffsets(int requiredCount)
        {
            _slotOffsets ??= new List<Vector2Int>();

            while (_slotOffsets.Count < requiredCount)
            {
                if (!TryGenerateNextOffset(out Vector2Int nextOffset))
                    break;

                _slotOffsets.Add(nextOffset);
            }
        }

        private bool TryGenerateNextOffset(out Vector2Int nextOffset)
        {
            _slotOffsets ??= new List<Vector2Int>();

            int depth = 1;
            while (depth < 128)
            {
                int lateralRange = Mathf.Max(1, depth);
                for (int lateral = 0; lateral <= lateralRange; lateral++)
                {
                    if (TryUseCandidate(new Vector2Int(lateral, depth), out nextOffset))
                        return true;

                    if (lateral > 0 && TryUseCandidate(new Vector2Int(-lateral, depth), out nextOffset))
                        return true;
                }

                depth++;
            }

            nextOffset = Vector2Int.zero;
            return false;
        }

        private bool TryUseCandidate(Vector2Int candidate, out Vector2Int nextOffset)
        {
            if (_slotOffsets.Contains(candidate))
            {
                nextOffset = Vector2Int.zero;
                return false;
            }

            nextOffset = candidate;
            return true;
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
    [SerializeField] private Necromancer _necromancer;

    private readonly List<GameObject> _spawnedPartyUnits = new();
    private CombatRoomController _currentCombatRoomController;
    private bool _isSubscribedToCombatController;

    public void Configure(FloorManager floorManager, NecromancerParty party, Necromancer necromancer = null)
    {
        _floorManager = floorManager;
        _party = party;

        if (necromancer != null)
            _necromancer = necromancer;
    }

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
        RefreshCombatRoomSubscription();
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
        UnsubscribeFromCurrentCombatController();
    }

    private void Start()
    {
        if (_floorManager != null && _floorManager.CurrentRoom != null)
        {
            DeployToRoom(_floorManager.CurrentRoom);
            RefreshCombatRoomSubscription(_floorManager.CurrentRoom);
        }
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
        RefreshCombatRoomSubscription(newRoom);
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

        if (!RoomFormationPlacementUtility.TryResolveEntryFrame(grid, roomContext, enteredDoor, ResolveNecromancer(), out RoomPlacementFrame placementFrame))
            return;

        var reservedCells = new HashSet<Vector3Int> { placementFrame.AnchorCell };
        _temporaryFormation ??= new TemporaryFormationPattern();
        _temporaryFormation.EnsureDefaults();

        int spawnIndex = 0;
        foreach (PartyMemberData member in deployableMembers)
        {
            if (member == null || member.UnitDefinition == null || member.UnitDefinition.unitPrefab == null)
                continue;

            if (!TryResolveFormationCell(
                    grid,
                    placementFrame.AnchorCell,
                    placementFrame.ForwardDirection,
                    placementFrame.LateralDirection,
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
        return RoomPlacementCellSelectionUtility.TryFindFormationCell(
            grid,
            desiredCell,
            new RoomPlacementFrame(anchorCell, forwardDirection),
            reservedCells,
            maxSearchRadius: 3,
            out resultCell);
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

    private void RefreshCombatRoomSubscription(GameObject roomObject = null)
    {
        _floorManager ??= GetComponent<FloorManager>() ?? GetComponentInParent<FloorManager>(includeInactive: true);

        GameObject targetRoom = roomObject != null
            ? roomObject
            : _floorManager != null ? _floorManager.CurrentRoom : null;

        CombatRoomController nextController = null;
        if (targetRoom != null && targetRoom.TryGetComponent(out RoomContext roomContext))
            nextController = roomContext.CombatController;

        if (ReferenceEquals(_currentCombatRoomController, nextController))
            return;

        UnsubscribeFromCurrentCombatController();
        _currentCombatRoomController = nextController;
        SubscribeToCurrentCombatController();
    }

    private void SubscribeToCurrentCombatController()
    {
        if (_currentCombatRoomController == null || _isSubscribedToCombatController)
            return;

        _currentCombatRoomController.StateChanged += HandleCombatStateChanged;
        _isSubscribedToCombatController = true;
    }

    private void UnsubscribeFromCurrentCombatController()
    {
        if (!_isSubscribedToCombatController || _currentCombatRoomController == null)
            return;

        _currentCombatRoomController.StateChanged -= HandleCombatStateChanged;
        _currentCombatRoomController = null;
        _isSubscribedToCombatController = false;
    }

    private void HandleCombatStateChanged(CombatRoomController controller, CombatRoomState state)
    {
        if (!ReferenceEquals(controller, _currentCombatRoomController))
            return;

        if (state != CombatRoomState.Resolved || controller.Outcome != CombatRoomOutcome.PlayerVictory)
            return;

        ClearCurrentDeployment();
    }

    public void TrackDeployment(GameObject instance, string partyMemberId)
    {
        if (instance == null)
            return;

        if (!_spawnedPartyUnits.Contains(instance))
            _spawnedPartyUnits.Add(instance);

        _party?.MarkDeployed(partyMemberId, true);
    }

    private Necromancer ResolveNecromancer()
    {
        _necromancer = NecromancerReferenceUtility.Resolve(_necromancer, this);
        return _necromancer;
    }

    private static bool IsCombatRoom(GameObject roomObject)
    {
        if (roomObject == null || !roomObject.TryGetComponent(out RoomPrefabProfile profile))
            return false;

        return RoomCombatUtility.IsCombatRoom(profile);
    }
}
