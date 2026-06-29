using PrefabDungeonGeneration;
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

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        ResolveFitter();
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
            return;
        }

        FitToActiveSceneBounds();
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

        if (TryFitExplicitRoomBounds(roomGO))
            return;

        if (TryFitRoomGridBounds(roomGO))
            return;

        Debug.LogWarning(
            $"[RoomCameraAdapter] Room '{roomGO.name}' has no RoomCameraBounds and no RoomGrid bounds. " +
            "Add a child GameObject with BoxCollider2D + RoomCameraBounds, or configure RoomGrid tilemaps, " +
            "to frame this room. Camera will not be repositioned.",
            this);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private bool TryFitExplicitRoomBounds(GameObject roomGO)
    {
        RoomCameraBounds bounds = roomGO.GetComponentInChildren<RoomCameraBounds>(includeInactive: false);
        if (bounds == null)
            return false;

        _fitter.FitToRoomBounds(bounds);
        return true;
    }

    private bool TryFitRoomGridBounds(GameObject roomGO)
    {
        RoomGrid roomGrid = ResolveRoomGrid(roomGO);
        if (roomGrid == null)
            return false;

        if (!roomGrid.TryGetWorldBounds(out Bounds worldBounds))
        {
            Debug.LogWarning(
                $"[RoomCameraAdapter] RoomGrid in room '{roomGO.name}' could not provide world bounds. " +
                "Check that its walkable tilemap is configured and contains tiles.",
                this);
            return false;
        }

        _fitter.FitToBounds(worldBounds);
        return true;
    }

    private static RoomGrid ResolveRoomGrid(GameObject roomGO)
    {
        if (roomGO == null)
            return null;

        RoomContext roomContext = roomGO.GetComponent<RoomContext>();
        if (roomContext != null && roomContext.RoomGrid != null)
            return roomContext.RoomGrid;

        return roomGO.GetComponentInChildren<RoomGrid>(includeInactive: false);
    }

    private void FitToActiveSceneBounds()
    {
        RoomCameraBounds bounds = FindFirstObjectByType<RoomCameraBounds>(FindObjectsInactive.Exclude);
        if (bounds == null)
            return;

        _fitter.FitToRoomBounds(bounds);
    }

    private void HandleRoomEntered(RoomDoor door, GameObject nextRoom)
    {
        SmoothFitToRoom(nextRoom);
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

    // -------------------------------------------------------------------------
    // Smooth transition (door-to-door)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Smoothly transitions the camera to frame <paramref name="roomGO"/>.
    /// Used when the player walks through a door.
    /// </summary>
    private void SmoothFitToRoom(GameObject roomGO)
    {
        if (roomGO == null)
        {
            Debug.LogWarning("[RoomCameraAdapter] SmoothFitToRoom called with null room.", this);
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

        if (TrySmoothFitExplicitRoomBounds(roomGO))
            return;

        if (TrySmoothFitRoomGridBounds(roomGO))
            return;

        // Fallback: no bounds found, log and do nothing (same as FitToRoom).
        Debug.LogWarning(
            $"[RoomCameraAdapter] Room '{roomGO.name}' has no RoomCameraBounds and no RoomGrid bounds. " +
            "Camera will not be repositioned.",
            this);
    }

    private bool TrySmoothFitExplicitRoomBounds(GameObject roomGO)
    {
        RoomCameraBounds bounds = roomGO.GetComponentInChildren<RoomCameraBounds>(includeInactive: false);
        if (bounds == null)
            return false;

        _fitter.SmoothFitToRoomBounds(bounds);
        return true;
    }

    private bool TrySmoothFitRoomGridBounds(GameObject roomGO)
    {
        RoomGrid roomGrid = ResolveRoomGrid(roomGO);
        if (roomGrid == null)
            return false;

        if (!roomGrid.TryGetWorldBounds(out Bounds worldBounds))
            return false;

        _fitter.SmoothFitToBounds(worldBounds);
        return true;
    }
}
