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

            if (debugInteractionFlow)
            {
                Debug.Log(
                    $"[SelectionInputHandler] Right-click interaction at {Input.mousePosition}. " +
                    $"interactableLayer={interactableLayer.value}, selectableLayer={selectableLayer.value}",
                    this);
            }

            if (!PointerInteractableResolver.TryResolveFromScreenPoint(
                    mainCamera,
                    Input.mousePosition,
                    interactableLayer,
                    selectableLayer,
                    out RaycastHit2D hit,
                    out IInteractable interactable))
            {
                if (debugInteractionFlow)
                    Debug.Log("[SelectionInputHandler] Interaction raycast hit nothing interactable.", this);

                return;
            }

            if (debugInteractionFlow)
            {
                Debug.Log(
                    $"[SelectionInputHandler] Interaction raycast hit collider '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.",
                    hit.collider);
            }

            if (!interactable.IsInteractionAvailable)
            {
                if (debugInteractionFlow)
                {
                    Debug.Log(
                        $"[SelectionInputHandler] Interactable '{((Component)interactable).name}' is not available. Interaction aborted.",
                        hit.collider);
                }

                return;
            }

            if (debugInteractionFlow)
            {
                Debug.Log(
                    $"[SelectionInputHandler] Resolved interactable '{((Component)interactable).name}'. Invoking Interact().",
                    hit.collider);
            }

            interactable.Interact();
        }
    }
}
