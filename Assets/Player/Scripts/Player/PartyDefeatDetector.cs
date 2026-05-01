using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PartyDefeatDetector : MonoBehaviour
{
    [SerializeField] private NecromancerParty _party;
    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private bool _debugLogs = true;

    private CombatRoomController _currentCombatRoomController;
    private bool _isSubscribedToController;

    public event Action PartyDefeated;

    public void Configure(NecromancerParty party, FloorManager floorManager)
    {
        _party = party;
        _floorManager = floorManager;
        RefreshCombatRoomSubscription();
    }

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
        RefreshCombatRoomSubscription();
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
        UnsubscribeFromCurrentController();
    }

    private void HandleRoomEntered(RoomDoor door, GameObject newRoom)
    {
        RefreshCombatRoomSubscription(newRoom);
    }

    private void RefreshCombatRoomSubscription(GameObject roomObject = null)
    {
        if (_floorManager == null)
            _floorManager = FindFirstObjectByType<FloorManager>();

        GameObject targetRoom = roomObject != null
            ? roomObject
            : _floorManager != null ? _floorManager.CurrentRoom : null;

        CombatRoomController nextController = null;
        if (targetRoom != null && targetRoom.TryGetComponent(out RoomContext roomContext))
            nextController = roomContext.CombatController;

        if (ReferenceEquals(_currentCombatRoomController, nextController))
            return;

        UnsubscribeFromCurrentController();
        _currentCombatRoomController = nextController;
        SubscribeToCurrentController();
    }

    private void SubscribeToCurrentController()
    {
        if (_currentCombatRoomController == null || _isSubscribedToController)
            return;

        _currentCombatRoomController.CombatResolved += HandleCombatResolved;
        _isSubscribedToController = true;
        LogDebug(
            $"[{nameof(PartyDefeatDetector)}] Listening to room '{_currentCombatRoomController.name}' " +
            $"for local defeat outcomes.");
    }

    private void UnsubscribeFromCurrentController()
    {
        if (!_isSubscribedToController || _currentCombatRoomController == null)
            return;

        _currentCombatRoomController.CombatResolved -= HandleCombatResolved;
        _isSubscribedToController = false;
        _currentCombatRoomController = null;
    }

    private void HandleCombatResolved(CombatRoomController controller, CombatRoomOutcome outcome)
    {
        if (!ReferenceEquals(controller, _currentCombatRoomController))
            return;

        if (outcome != CombatRoomOutcome.PlayerDefeat && outcome != CombatRoomOutcome.MutualDefeat)
            return;

        LogDebug(
            $"[{nameof(PartyDefeatDetector)}] Party defeat triggered from room '{controller.name}' " +
            $"with outcome {outcome}.");
        PartyDefeated?.Invoke();
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
