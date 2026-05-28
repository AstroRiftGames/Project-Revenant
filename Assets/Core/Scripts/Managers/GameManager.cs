using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameStateManager _stateManager;
    public GameStateManager StateManager => _stateManager;

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
            return;
        }

        if (_stateManager == null)
        {
            _stateManager = GetComponent<GameStateManager>();
            if (_stateManager == null)
            {
                _stateManager = gameObject.AddComponent<GameStateManager>();
            }
        }

        RequestStateChange(GameState.SafeZone);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FloorManager.OnRoomEntered += OnRoomEntered;
        CombatRoomController.AnyCombatResolved += OnAnyCombatResolved;
        BaseStation.OnStationUIRequestedGlobal += OnStationUIRequestedGlobal;
        StationUIManager.OnAnyStationClosed += OnStationClosed;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FloorManager.OnRoomEntered -= OnRoomEntered;
        CombatRoomController.AnyCombatResolved -= OnAnyCombatResolved;
        BaseStation.OnStationUIRequestedGlobal -= OnStationUIRequestedGlobal;
        StationUIManager.OnAnyStationClosed -= OnStationClosed;
    }

    public void RequestStateChange(GameState nextState)
    {
        if (_stateManager != null)
        {
            _stateManager.TryTransitionTo(nextState);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SafeZone")
        {
            RequestStateChange(GameState.SafeZone);
        }
        else if (scene.name == "Dungeon")
        {
            RequestStateChange(GameState.ExploringDungeon);
        }
    }

    private void OnRoomEntered(RoomDoor door, GameObject nextRoom)
    {
        if (nextRoom != null && nextRoom.TryGetComponent(out RoomContext roomContext))
        {
            if (roomContext.IsCombatRoom)
            {
                RequestStateChange(GameState.Deployment);
                if (roomContext.CombatController != null)
                {
                    roomContext.CombatController.CombatStarted -= OnCombatStarted;
                    roomContext.CombatController.CombatStarted += OnCombatStarted;
                }
            }
            else
            {
                RequestStateChange(GameState.ExploringDungeon);
            }
        }
    }

    private void OnCombatStarted(CombatRoomController controller)
    {
        RequestStateChange(GameState.InCombat);
    }

    private void OnAnyCombatResolved(CombatRoomController controller, CombatRoomOutcome outcome)
    {
        if (outcome == CombatRoomOutcome.PlayerVictory)
        {
            RequestStateChange(GameState.CombatResolved);
            RequestStateChange(GameState.ExploringDungeon);
        }
        else
        {
            RequestStateChange(GameState.GameOver);
        }
    }

    private void OnStationUIRequestedGlobal(BaseStation station, UIType uiType)
    {
        RequestStateChange(GameState.StationUI);
    }

    private void OnStationClosed()
    {
        if (SceneManager.GetActiveScene().name == "SafeZone")
            RequestStateChange(GameState.SafeZone);
        else
            RequestStateChange(GameState.ExploringDungeon);
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
