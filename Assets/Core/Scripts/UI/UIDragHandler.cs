using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UI
{
    /// <summary>
    /// Allows dragging a UI element. 
    /// Attach this to the part of the UI you want to be the "handle" (e.g., Title Bar)
    /// and assign the main panel to targetTransform.
    /// </summary>
    public class UIDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        [SerializeField] private RectTransform targetTransform;
        
        private Canvas _canvas;
        private Vector2 _offset;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = GetComponent<RectTransform>();
            }
            
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Calculate offset between mouse and panel center to avoid "jumping"
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out _offset
            );
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null || targetTransform == null) return;

            // Move the panel
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)targetTransform.parent, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPoint))
            {
                targetTransform.localPosition = localPoint - _offset;
            }
        }
    }
}
