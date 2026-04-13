using UnityEngine;
using Selection.Interfaces;

namespace Selection.Core
{
    public class SelectionInputHandler : MonoBehaviour
    {
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private LayerMask selectableLayer;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool debugInteractionFlow;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            if (selectionManager == null)
            {
                selectionManager = GetComponent<SelectionManager>();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleSelectionClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                HandleInteractionClick();
            }
        }

        private void HandleSelectionClick()
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, selectableLayer);

            if (hit.collider != null)
            {
                ISelectable selectable = hit.collider.GetComponentInParent<ISelectable>();
                if (selectable != null)
                {
                    selectionManager.ToggleSelection(selectable);
                }
            }
        }

        private void HandleInteractionClick()
        {
            if (mainCamera == null)
            {
                if (debugInteractionFlow)
                    Debug.LogWarning("[SelectionInputHandler] Interaction click ignored because mainCamera is null.", this);

                return;
            }

            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            int fallbackInteractableMask = LayerMask.GetMask("Interactable");
            int interactionMask = interactableLayer.value != 0
                ? interactableLayer.value | selectableLayer.value
                : selectableLayer.value | fallbackInteractableMask;

            if (debugInteractionFlow)
            {
                Debug.Log(
                    $"[SelectionInputHandler] Right-click interaction at {mousePosition}. " +
                    $"interactableLayer={interactableLayer.value}, selectableLayer={selectableLayer.value}, effectiveMask={interactionMask}",
                    this);
            }

            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, interactionMask);

            if (hit.collider == null)
            {
                if (debugInteractionFlow)
                    Debug.Log("[SelectionInputHandler] Interaction raycast hit nothing.", this);

                return;
            }

            if (debugInteractionFlow)
            {
                Debug.Log(
                    $"[SelectionInputHandler] Interaction raycast hit collider '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.",
                    hit.collider);
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (debugInteractionFlow)
            {
                Debug.Log(
                    interactable != null
                        ? $"[SelectionInputHandler] Resolved interactable '{((Component)interactable).name}'. Invoking Interact()."
                        : "[SelectionInputHandler] Hit collider does not resolve to any IInteractable in parents.",
                    hit.collider);
            }

            interactable?.Interact();
        }
    }
}
