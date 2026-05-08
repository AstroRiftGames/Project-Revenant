using UnityEngine;
using UnityEngine.EventSystems;

namespace Selection.UI
{
    /// <summary>
    /// Script to be added to the CreatureSelectionCard prefab.
    /// Handles left-click interaction to open the Deep Inspector panel via UIManager.
    /// </summary>
    public class DeepInspectorTrigger : MonoBehaviour, IPointerClickHandler
    {
        private CharacterSelectionUIEntry _uiEntry;

        private void Awake()
        {
            _uiEntry = GetComponent<CharacterSelectionUIEntry>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only trigger on left click as per requirements
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (DeepInspector.Instance != null && _uiEntry != null && _uiEntry.Stats != null)
                {
                    DeepInspector.Instance.Display(_uiEntry.Stats);
                }
                else if (UIManager.Instance != null)
                {
                    // Fallback if DeepInspector Singleton is not ready or data is missing
                    UIManager.Instance.ShowElement(UIType.DeepInspector);
                }
            }
        }
    }
}
