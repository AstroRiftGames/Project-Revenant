using UnityEngine;

/// <summary>
/// Marks the camera-framing bounds of a room using a BoxCollider2D as the editable region.
/// Place this on a child GameObject of the room prefab named "RoomCameraBounds".
///
/// Setup:
///  1. Add a BoxCollider2D and size it to cover the full room (floor + visible walls).
///  2. Set the GameObject's Layer to "CameraBounds" (create it in Project Settings > Tags and Layers).
///  3. In Physics 2D Layer Collision Matrix, uncheck all CameraBounds intersections.
///
/// The collider is always a trigger, so it never affects physics, pathfinding, or raycasts.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class RoomCameraBounds : MonoBehaviour
{
    private const string RecommendedLayerName = "CameraBounds";

    private BoxCollider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        EnforceTriggger();
        ValidateLayer();
    }

    /// <summary>World-space bounds of the room area, driven by the BoxCollider2D.</summary>
    public Bounds Bounds
    {
        get
        {
            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();

            if (_collider == null)
            {
                Debug.LogWarning(
                    $"[RoomCameraBounds] '{name}': BoxCollider2D is missing. " +
                    "Add one and size it to cover the full room.",
                    this);
                return new Bounds(transform.position, Vector3.zero);
            }

            return _collider.bounds;
        }
    }

    private void EnforceTriggger()
    {
        if (_collider != null && !_collider.isTrigger)
        {
            _collider.isTrigger = true;
            Debug.LogWarning(
                $"[RoomCameraBounds] '{name}': BoxCollider2D was not a trigger. " +
                "Forced isTrigger = true to prevent physics interactions.",
                this);
        }
    }

    private void ValidateLayer()
    {
        int recommendedLayer = LayerMask.NameToLayer(RecommendedLayerName);
        if (recommendedLayer >= 0 && gameObject.layer != recommendedLayer)
        {
            Debug.LogWarning(
                $"[RoomCameraBounds] '{name}': GameObject layer is not '{RecommendedLayerName}'. " +
                $"Current layer: '{LayerMask.LayerToName(gameObject.layer)}'. " +
                $"Set it to '{RecommendedLayerName}' via Project Settings > Tags and Layers, " +
                "then assign it here to prevent unintended physics interactions.",
                this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f); // yellow fill
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawCube(col.offset, col.size);

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f); // yellow wire
        Gizmos.DrawWireCube(col.offset, col.size);

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
