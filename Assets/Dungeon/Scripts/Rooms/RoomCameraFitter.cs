using UnityEngine;

/// <summary>
/// Calculates and applies orthographic size + position so that a world-space Bounds
/// fits entirely inside a normalized <see cref="PlayableViewport"/>, leaving room for HUD elements.
///
/// Attach to the same GameObject as the main Camera (or any persistent object).
/// Assign _camera in the Inspector, or leave it null to fall back to Camera.main.
///
/// The playable viewport is a normalized Rect (0–1 in both axes) that represents
/// the screen area free of UI. Example:
///   new Rect(0.02f, 0.16f, 0.76f, 0.82f)
///   → starts 2 % from the left, 16 % from the bottom, 76 % wide, 82 % tall.
///   → leaves space for a bottom HUD and a right-side panel.
/// </summary>
public sealed class RoomCameraFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;

    [Header("Playable Viewport (normalized 0-1)")]
    [Tooltip("Normalized screen rect that represents the UI-free play area.\n" +
             "x/y = bottom-left corner, width/height = size.\n" +
             "Default leaves space for a bottom HUD and a right panel.")]
    [SerializeField] private Rect _playableViewport = new Rect(0.02f, 0.16f, 0.76f, 0.82f);

    [Header("Fit Settings")]
    [SerializeField] private float _padding = 1.5f;
    [SerializeField] private float _minOrthographicSize = 5f;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Fits the camera so <paramref name="bounds"/> fills the playable viewport.</summary>
    public void FitToBounds(Bounds worldBounds)
    {
        Camera cam = ResolveCamera();
        if (cam == null) return;
        if (!ValidateViewport()) return;

        float effectiveAspect = (cam.aspect * _playableViewport.width) / _playableViewport.height;

        float verticalHalf   = worldBounds.size.y * 0.5f;
        float horizontalHalf = worldBounds.size.x * 0.5f / effectiveAspect;

        float targetSize = Mathf.Max(verticalHalf, horizontalHalf) + _padding;
        cam.orthographicSize = Mathf.Max(targetSize, _minOrthographicSize);

        cam.transform.position = new Vector3(
            CalculateCameraCenter(worldBounds.center, cam).x,
            CalculateCameraCenter(worldBounds.center, cam).y,
            cam.transform.position.z);
    }

    /// <summary>Convenience overload — reads Bounds from a <see cref="RoomCameraBounds"/>.</summary>
    public void FitToRoomBounds(RoomCameraBounds roomBounds)
    {
        if (roomBounds == null)
        {
            Debug.LogWarning(
                "[RoomCameraFitter] FitToRoomBounds called with a null RoomCameraBounds.",
                this);
            return;
        }

        FitToBounds(roomBounds.Bounds);
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("Fit To Bounds (Editor / Play Mode)")]
    private void FitToBoundsEditor()
    {
        // Requires a RoomCameraBounds somewhere in the scene to preview the fit.
        RoomCameraBounds bounds = FindFirstObjectByType<RoomCameraBounds>();
        if (bounds == null)
        {
            Debug.LogWarning(
                "[RoomCameraFitter] No RoomCameraBounds found in the scene. " +
                "Cannot preview fit.",
                this);
            return;
        }

        FitToRoomBounds(bounds);
        Debug.Log($"[RoomCameraFitter] Fit applied using '{bounds.name}'.", this);
    }
#endif

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private Camera ResolveCamera()
    {
        if (_camera != null)
            return _camera;

        _camera = Camera.main;

        if (_camera == null)
        {
            Debug.LogWarning(
                "[RoomCameraFitter] No camera assigned and Camera.main is null. " +
                "Assign a Camera in the Inspector.",
                this);
        }

        return _camera;
    }

    private bool ValidateViewport()
    {
        if (_playableViewport.width <= 0f || _playableViewport.height <= 0f)
        {
            Debug.LogWarning(
                $"[RoomCameraFitter] PlayableViewport has invalid dimensions: {_playableViewport}. " +
                "Width and Height must be greater than 0.",
                this);
            return false;
        }

        Camera cam = ResolveCamera();
        if (cam != null && !cam.orthographic)
        {
            Debug.LogWarning(
                "[RoomCameraFitter] Camera is not orthographic. " +
                "RoomCameraFitter only supports orthographic cameras.",
                this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the world position the camera should be at so the room center
    /// appears centered inside the playable viewport (not the full screen).
    /// </summary>
    private Vector3 CalculateCameraCenter(Vector3 roomWorldCenter, Camera cam)
    {
        // Offset from screen center to playable viewport center (in viewport space).
        Vector2 viewportCenter = _playableViewport.center;
        Vector2 screenCenter   = new Vector2(0.5f, 0.5f);
        Vector2 viewportOffset = viewportCenter - screenCenter;

        // Convert viewport offset to world units.
        float cameraHeight = cam.orthographicSize * 2f;
        float cameraWidth  = cameraHeight * cam.aspect;

        Vector3 worldOffset = new Vector3(
            viewportOffset.x * cameraWidth,
            viewportOffset.y * cameraHeight,
            0f);

        // The camera must be shifted opposite to the offset so the room appears centered.
        return roomWorldCenter - worldOffset;
    }
}
