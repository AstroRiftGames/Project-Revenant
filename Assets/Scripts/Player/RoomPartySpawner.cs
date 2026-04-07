using System.Collections.Generic;
using System.Linq;
using PrefabDungeonGeneration;
using UnityEngine;

public class RoomPartySpawner : MonoBehaviour
{
    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private NecromancerParty _party;
    [SerializeField] private int _spawnEdgePadding = 1;

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

    private void HandleRoomEntered(RoomDoor door, GameObject newRoom)
    {
        DeployToRoom(newRoom);
    }

    private void DeployToRoom(GameObject roomObject)
    {
        ClearCurrentDeployment();

        if (_party == null || roomObject == null || !roomObject.TryGetComponent(out RoomContext roomContext))
            return;

        if (!IsCombatRoom(roomObject))
            return;

        List<Vector3Int> spawnCells = roomContext.GetAvailableSpawnCells(_spawnEdgePadding);
        if (spawnCells.Count == 0)
            return;

        spawnCells = spawnCells.OrderBy(cell => cell.x).ThenBy(cell => cell.y).ToList();

        int spawnIndex = 0;
        foreach (PartyMemberData member in _party.GetDeployableMembers())
        {
            if (member == null || member.UnitData == null || member.UnitData.unitPrefab == null)
                continue;

            if (spawnIndex >= spawnCells.Count)
                break;

            Vector3 spawnPosition = roomContext.BattleGrid.CellToWorld(spawnCells[spawnIndex]);
            GameObject instance = Instantiate(member.UnitData.unitPrefab, spawnPosition, Quaternion.identity, roomContext.transform);

            if (!instance.TryGetComponent(out Unit unit))
            {
                Debug.LogWarning($"[RoomPartySpawner] '{member.UnitData.name}' prefab does not contain Unit.", this);
                Destroy(instance);
                continue;
            }

            PartyMemberLink link = instance.GetComponent<PartyMemberLink>();
            if (link == null)
                link = instance.AddComponent<PartyMemberLink>();

            link.Initialize(member.PartyMemberId, true);
            instance.GetComponent<LifeController>()?.SetCurrentHealth(Mathf.Max(1, member.CurrentHealth));

            _party.MarkDeployed(member.PartyMemberId, true);
            _spawnedPartyUnits.Add(instance);
            spawnIndex++;
        }
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

    private static bool IsCombatRoom(GameObject roomObject)
    {
        if (!roomObject.TryGetComponent(out RoomPrefabProfile profile) || profile == null)
            return false;

        return profile.RoomType == PDRoomType.Combat ||
               profile.RoomType == PDRoomType.MiniBoss ||
               profile.RoomType == PDRoomType.Boss;
    }
}
