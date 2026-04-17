using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Necromancer))]
public class NecromancerCombatStartAdapter : MonoBehaviour
{
    [SerializeField] private Necromancer _necromancer;
    [SerializeField] private KeyCode _startCombatKey = KeyCode.F;
    [SerializeField] private bool _debugLogs = true;

    private void Awake()
    {
        _necromancer ??= GetComponent<Necromancer>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_startCombatKey))
            return;

        TryStartCombatInCurrentRoom();
    }

    public bool TryStartCombatInCurrentRoom()
    {
        if (_necromancer == null)
        {
            LogDebug($"[{nameof(NecromancerCombatStartAdapter)}] Missing {nameof(Necromancer)} reference.");
            return false;
        }

        if (!TryResolveCurrentRoomContext(out RoomContext roomContext))
        {
            LogDebug($"[{nameof(NecromancerCombatStartAdapter)}] Could not resolve current RoomContext.");
            return false;
        }

        if (!roomContext.IsCombatRoom)
        {
            LogDebug($"[{nameof(NecromancerCombatStartAdapter)}] Room '{roomContext.name}' is not a combat room.");
            return false;
        }

        CombatRoomController combatController = roomContext.CombatController;
        if (combatController == null)
        {
            LogDebug($"[{nameof(NecromancerCombatStartAdapter)}] Combat room '{roomContext.name}' has no CombatRoomController.");
            return false;
        }

        bool started = combatController.TryStartCombat();
        LogDebug(
            $"[{nameof(NecromancerCombatStartAdapter)}] Start combat requested in room '{roomContext.name}'. " +
            $"Started: {started}. Current state: {combatController.State}.");
        return started;
    }

    private bool TryResolveCurrentRoomContext(out RoomContext roomContext)
    {
        roomContext = null;
        if (!_necromancer.TryGetGrid(out RoomGrid grid) || grid == null)
            return false;

        roomContext = grid.GetComponentInParent<RoomContext>(includeInactive: true);
        return roomContext != null;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
