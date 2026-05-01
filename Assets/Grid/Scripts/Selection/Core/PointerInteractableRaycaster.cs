using UnityEngine;

namespace Selection.Core
{
    internal static class PointerInteractableResolver
    {
        public static bool TryResolveFromScreenPoint(
            Camera camera,
            Vector2 screenPosition,
            LayerMask interactableLayer,
            LayerMask selectableLayer,
            out RaycastHit2D hit,
            out IInteractable interactable)
        {
            interactable = null;
            hit = default;

            if (camera == null)
                return false;

            Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
            int fallbackInteractableMask = LayerMask.GetMask("Interactable");
            int interactionMask = interactableLayer.value != 0
                ? interactableLayer.value | selectableLayer.value
                : selectableLayer.value | fallbackInteractableMask;

            return TryResolveFromWorldPoint(worldPosition, interactionMask, out hit, out interactable);
        }

        public static bool TryResolveFromWorldPoint(
            Vector2 worldPosition,
            int layerMask,
            out RaycastHit2D hit,
            out IInteractable interactable)
        {
            interactable = null;
            hit = default;

            if (layerMask == 0)
                return false;

            hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, layerMask);
            if (hit.collider == null)
                return false;

            interactable = hit.collider.GetComponentInParent<IInteractable>();
            return interactable != null;
        }
    }
}
