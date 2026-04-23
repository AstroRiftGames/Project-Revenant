using System;
using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Header("Connected Rooms")]
    public GameObject roomA;
    public GameObject roomB;

    private RoomContext _roomContext;
    private CombatRoomController _combatRoomController;

    public static event Action<RoomDoor> OnDoorInteracted;

    public bool IsCombatLocked => IsLockedByActiveCombat();

    public void TriggerStructuralInteraction()
    {
        PerformInteraction();
    }

    protected virtual void PerformInteraction()
    {
        OnDoorInteracted?.Invoke(this);
    }

    private void ResolveRoomDependencies()
    {
        _roomContext ??= GetComponentInParent<RoomContext>(includeInactive: true);
        _combatRoomController ??= _roomContext != null ? _roomContext.CombatController : null;
    }

    private bool IsLockedByActiveCombat()
    {
        ResolveRoomDependencies();

        return _combatRoomController != null &&
               _combatRoomController.IsCombatRoom &&
               !_combatRoomController.IsResolved;
    }
}
