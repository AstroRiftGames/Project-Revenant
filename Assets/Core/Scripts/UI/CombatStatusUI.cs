using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI element that displays the number of alive allies and enemies in the current room.
/// </summary>
public class CombatStatusUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _alliesCountText;
    [SerializeField] private TextMeshProUGUI _enemiesCountText;

    private RoomContext _currentRoom;

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
        LifeController.OnUnitDied += HandleUnitDied;
        Creature.OnCreatureEnabled += HandleCreatureEnabled;
        
        RefreshCurrentRoom();
        UpdateDisplay();
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
        LifeController.OnUnitDied -= HandleUnitDied;
        Creature.OnCreatureEnabled -= HandleCreatureEnabled;
    }

    private void HandleRoomEntered(RoomDoor door, GameObject room)
    {
        if (room.TryGetComponent(out RoomContext context))
        {
            _currentRoom = context;
            UpdateDisplay();
        }
    }

    private void HandleUnitDied(Unit unit)
    {
        // We always update on death to keep it accurate, 
        // even if the unit is not in the current room (though it should be).
        UpdateDisplay();
    }

    private void HandleCreatureEnabled(Creature creature)
    {
        // When a new creature is enabled (spawned/summoned), update display.
        UpdateDisplay();
    }

    private void RefreshCurrentRoom()
    {
        // Attempt to find the current room if we don't have one (e.g. on initialization)
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        if (floorManager != null && floorManager.CurrentRoom != null)
        {
            _currentRoom = floorManager.CurrentRoom.GetComponent<RoomContext>();
        }
    }

    public void UpdateDisplay()
    {
        int allies = 0;
        int enemies = 0;

        if (_currentRoom != null)
        {
            IReadOnlyList<Unit> units = _currentRoom.Units;
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (unit == null || !unit.gameObject.activeInHierarchy || !unit.IsAlive)
                    continue;

                if (unit.Team == UnitTeam.NecromancerAlly)
                    allies++;
                else if (unit.Team == UnitTeam.Enemy)
                    enemies++;
            }
        }

        if (_alliesCountText != null)
            _alliesCountText.text = allies.ToString();
            
        if (_enemiesCountText != null)
            _enemiesCountText.text = enemies.ToString();
    }
}
