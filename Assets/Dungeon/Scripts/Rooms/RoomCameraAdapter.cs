using PrefabDungeonGeneration;
using ProceduralDungeon;
using UnityEngine;

/// <summary>
/// Bridges the room-transition event from <see cref="FloorManager"/> with
/// <see cref="RoomCameraFitter"/>, automatically framing each room when the
/// player enters it.
///
/// Attach to the same GameObject as <see cref="RoomCameraFitter"/> (usually
/// the main Camera GameObject). Wire <see cref="_fitter"/> in the Inspector.
///
/// This component has NO dependency on UI, RoomGrid, units, or VFX.
/// It only listens to FloorManager.OnRoomEntered and delegates to the fitter.
/// </summary>
[RequireComponent(typeof(RoomCameraFitter))]
public sealed class RoomCameraAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomCameraFitter _fitter;

    [Tooltip("Optional. If assigned, this follow script will be disabled when the camera frames a room " +
             "to prevent it from overriding the fitted position every LateUpdate.")]
    [SerializeField] private IsometricCameraFollow _cameraFollow;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        ResolveFitter();
        ResolveCameraFollow();
    }

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
        PrefabDungeonGenerator.OnFloorGenerated += HandleFloorGenerated;
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
        PrefabDungeonGenerator.OnFloorGenerated -= HandleFloorGenerated;
    }

    private void Start()
    {
        // Attempt to frame the room if it was already set (e.g. static scene or generated before Start)
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        if (floorManager != null && floorManager.CurrentRoom != null)
        {
            FitToRoom(floorManager.CurrentRoom);
        }
    }

    // -------------------------------------------------------------------------
    // Public API — can also be called manually (e.g. from a scene bootstrap)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Immediately fits the camera to the bounds of <paramref name="roomGO"/>.
    /// Logs a warning if no <see cref="RoomCameraBounds"/> is found.
    /// </summary>
    public void FitToRoom(GameObject roomGO)
    {
        if (roomGO == null)
        {
            Debug.LogWarning("[RoomCameraAdapter] FitToRoom called with null room.", this);
            return;
        }

        if (_fitter == null)
        {
            Debug.LogWarning(
                "[RoomCameraAdapter] RoomCameraFitter reference is missing. " +
                "Assign it in the Inspector.",
                this);
            return;
        }

        RoomCameraBounds bounds = roomGO.GetComponentInChildren<RoomCameraBounds>(includeInactive: false);

        if (bounds == null)
        {
            Debug.LogWarning(
                $"[RoomCameraAdapter] Room '{roomGO.name}' has no RoomCameraBounds child. " +
                "Add a child GameObject with BoxCollider2D + RoomCameraBounds to frame this room. " +
                "Camera will not be repositioned.",
                this);
            return;
        }

        DisableCameraFollow();
        _fitter.FitToRoomBounds(bounds);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void HandleRoomEntered(RoomDoor door, GameObject nextRoom)
    {
        FitToRoom(nextRoom);
    }

    private void HandleFloorGenerated(PDFloorData floorData)
    {
        // When the dungeon is generated, the FloorManager sets the initial room. Frame it.
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        if (floorManager != null && floorManager.CurrentRoom != null)
        {
            FitToRoom(floorManager.CurrentRoom);
        }
    }

    private void DisableCameraFollow()
    {
        if (_cameraFollow != null && _cameraFollow.enabled)
        {
            _cameraFollow.enabled = false;
            Debug.Log(
                "[RoomCameraAdapter] IsometricCameraFollow disabled to allow room framing. " +
                "Re-enable it manually if you need follow behaviour outside of rooms.",
                this);
        }
    }

    private void ResolveCameraFollow()
    {
        if (_cameraFollow == null)
            _cameraFollow = GetComponent<IsometricCameraFollow>();
    }

    private void ResolveFitter()
    {
        if (_fitter == null)
            _fitter = GetComponent<RoomCameraFitter>();

        if (_fitter == null)
        {
            Debug.LogWarning(
                "[RoomCameraAdapter] Could not find a RoomCameraFitter on this GameObject. " +
                "Add one and assign it in the Inspector.",
                this);
        }
    }
}
