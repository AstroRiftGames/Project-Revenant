using System.Collections.Generic;
using UnityEngine;

public enum UIType
{
    Inspector,
    Minimap,
    LOG,
    Currency,
    ProgressBar
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [System.Serializable]
    public class UIElement
    {
        public UIType ElementType;
        public GameObject ElementObject;
    }

    [Tooltip("List of UI Elements managed by the UIManager")]
    [SerializeField] private List<UIElement> _uiElements = new List<UIElement>();
    
    private Dictionary<UIType, GameObject> _uiDictionary = new Dictionary<UIType, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionary()
    {
        _uiDictionary.Clear();
        foreach (var element in _uiElements)
        {
            if (element.ElementObject != null)
            {
                if (!_uiDictionary.ContainsKey(element.ElementType))
                {
                    _uiDictionary.Add(element.ElementType, element.ElementObject);
                }
                else
                {
                    Debug.LogWarning($"[UIManager] Duplicate ElementType found: {element.ElementType}");
                }
            }
        }
    }

    /// <summary>
    /// Activates the UI element with the given type.
    /// </summary>
    public void ShowElement(UIType elementType)
    {
        if (_uiDictionary.TryGetValue(elementType, out GameObject obj))
        {
            obj.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[UIManager] Element not found or not assigned: {elementType}");
        }
    }

    /// <summary>
    /// Deactivates the UI element with the given type.
    /// </summary>
    public void HideElement(UIType elementType)
    {
        if (_uiDictionary.TryGetValue(elementType, out GameObject obj))
        {
            obj.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[UIManager] Element not found or not assigned: {elementType}");
        }
    }

    /// <summary>
    /// Toggles the active state of the UI element with the given type.
    /// </summary>
    public void ToggleElement(UIType elementType)
    {
        if (_uiDictionary.TryGetValue(elementType, out GameObject obj))
        {
            obj.SetActive(!obj.activeSelf);
        }
        else
        {
            Debug.LogWarning($"[UIManager] Element not found or not assigned: {elementType}");
        }
    }
}
