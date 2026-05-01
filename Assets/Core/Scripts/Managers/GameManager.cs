using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Commands the UIManager to show a specific UI element.
    /// </summary>
    public void RequestShowUI(UIType elementType)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowElement(elementType);
        }
        else
        {
            Debug.LogWarning("[GameManager] Cannot show UI, UIManager Instance is missing.");
        }
    }

    /// <summary>
    /// Commands the UIManager to hide a specific UI element.
    /// </summary>
    public void RequestHideUI(UIType elementType)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideElement(elementType);
        }
        else
        {
            Debug.LogWarning("[GameManager] Cannot hide UI, UIManager Instance is missing.");
        }
    }

    /// <summary>
    /// Commands the UIManager to toggle a specific UI element.
    /// </summary>
    public void RequestToggleUI(UIType elementType)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleElement(elementType);
        }
        else
        {
            Debug.LogWarning("[GameManager] Cannot toggle UI, UIManager Instance is missing.");
        }
    }
}
