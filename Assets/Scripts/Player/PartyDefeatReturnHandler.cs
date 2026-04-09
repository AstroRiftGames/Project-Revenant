using System.Collections;
using PrefabDungeonGeneration;
using UnityEngine;

[DisallowMultipleComponent]
public class PartyDefeatReturnHandler : MonoBehaviour
{
    [SerializeField] private PartyDefeatDetector _detector;
    [SerializeField] private FloorManager _floorManager;
    [SerializeField] private RoomPartySpawner _partySpawner;
    [SerializeField] private NecromancerRoomTransitioner _roomTransitioner;
    [SerializeField] private PrefabDungeonGenerator _dungeonGenerator;

    private Coroutine _pendingReturnRoutine;
    private bool _isSubscribed;

    private void Awake()
    {
        RefreshSubscription();
    }

    private void OnEnable()
    {
        RefreshSubscription();
    }

    private void OnDisable()
    {
        if (_pendingReturnRoutine != null)
        {
            StopCoroutine(_pendingReturnRoutine);
            _pendingReturnRoutine = null;
        }

        Unsubscribe();
    }

    public void Configure(
        PartyDefeatDetector detector,
        FloorManager floorManager,
        RoomPartySpawner partySpawner,
        NecromancerRoomTransitioner roomTransitioner,
        PrefabDungeonGenerator dungeonGenerator)
    {
        Unsubscribe();

        _detector = detector;
        _floorManager = floorManager;
        _partySpawner = partySpawner;
        _roomTransitioner = roomTransitioner;
        _dungeonGenerator = dungeonGenerator;

        RefreshSubscription();
    }

    private void HandlePartyDefeated()
    {
        if (_pendingReturnRoutine != null)
            return;

        _pendingReturnRoutine = StartCoroutine(ReturnToStartRoomNextFrame());
    }

    private IEnumerator ReturnToStartRoomNextFrame()
    {
        yield return null;

        _pendingReturnRoutine = null;

        if (_floorManager == null)
        {
            Debug.LogWarning("[NecromancerPartyDefeatReturnToStartResolver] Cannot return to the start room because FloorManager is missing.", this);
            yield break;
        }

        if (_dungeonGenerator == null || _dungeonGenerator.LastGeneratedStartRoom == null)
        {
            Debug.LogWarning("[NecromancerPartyDefeatReturnToStartResolver] Cannot return to the start room because the current floor start room is unavailable.", this);
            yield break;
        }

        GameObject startRoom = _dungeonGenerator.LastGeneratedStartRoom;
        _floorManager.EnterRoom(startRoom);
        _roomTransitioner?.MoveNecromancerToRoom(startRoom);
        _partySpawner?.DeployToRoom(startRoom);
    }

    private void RefreshSubscription()
    {
        if (_detector == null)
            _detector = GetComponent<PartyDefeatDetector>();

        if (!isActiveAndEnabled || _detector == null || _isSubscribed)
            return;

        _detector.PartyDefeated += HandlePartyDefeated;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _detector == null)
            return;

        _detector.PartyDefeated -= HandlePartyDefeated;
        _isSubscribed = false;
    }
}
