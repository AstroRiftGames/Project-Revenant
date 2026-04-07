using UnityEngine;
using Selection.Interfaces;

namespace Selection.Core
{
    public class SelectionInputHandler : MonoBehaviour
    {
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private LayerMask selectableLayer;
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
    }
}
