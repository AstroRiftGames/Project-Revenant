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
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            int interactionMask = interactableLayer.value != 0 ? interactableLayer : selectableLayer;
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, interactionMask);

            if (hit.collider == null)
                return;

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact();
        }
    }
}
