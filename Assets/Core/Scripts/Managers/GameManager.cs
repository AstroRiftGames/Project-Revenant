using PrefabDungeonGeneration;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Systems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameStateManager _stateManager;
    public GameStateManager StateManager => _stateManager;

    private CombatRoomController _currentEncounterController;
    private GameState _stateBeforePause = GameState.SafeZone;

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
        PauseManager.OnPauseRequested += RequestPause;
        PauseManager.OnResumeRequested += RequestResume;
        PauseManager.OnQuitRequested += RequestQuit;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FloorManager.OnRoomEntered -= OnRoomEntered;
        ClearCurrentEncounterSubscription();
        CombatRoomController.AnyCombatResolved -= OnAnyCombatResolved;
        BaseStation.OnStationUIRequestedGlobal -= OnStationUIRequestedGlobal;
        StationUIManager.OnAnyStationClosed -= OnStationClosed;
        PauseManager.OnPauseRequested -= RequestPause;
        PauseManager.OnResumeRequested -= RequestResume;
    }

    public void RequestPause()
    {
        if (StateManager.CurrentState == GameState.Paused)
            return;

        Time.timeScale = 0f;
        _stateBeforePause = StateManager.CurrentState;
        RequestStateChange(GameState.Paused);
    }

    public void RequestResume()
    {
        if (StateManager.CurrentState != GameState.Paused)
            return;

        Time.timeScale = 1f;
        RequestStateChange(_stateBeforePause);
    }

    public void RequestQuit()
    {
        Debug.Log("Quit requested");
        Application.Quit();
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
        ClearCurrentEncounterSubscription();

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
        if (nextRoom == null || !nextRoom.TryGetComponent(out RoomContext roomContext))
            return;

        if (_currentEncounterController != null &&
            _currentEncounterController.IsCombatActive &&
            (roomContext.CombatController == null || !ReferenceEquals(roomContext.CombatController, _currentEncounterController)))
        {
            Debug.LogWarning(
                $"[{nameof(GameManager)}] Ignored room transition to '{nextRoom.name}' while encounter " +
                $"'{_currentEncounterController.name}' is still in combat.",
                this);
            return;
        }

        if (!roomContext.IsCombatRoom)
        {
            RefreshCurrentEncounterSubscription(null);
            RequestStateChange(GameState.ExploringDungeon);
            return;
        }

        RefreshCurrentEncounterSubscription(roomContext.CombatController);

        if (roomContext.CombatController != null && roomContext.CombatController.IsResolved)
        {
            RequestStateChange(GameState.ExploringDungeon);
            return;
        }

        if (roomContext.CombatController != null && roomContext.CombatController.IsCombatActive)
        {
            RequestStateChange(GameState.InCombat);
            return;
        }

        RequestStateChange(GameState.Deployment);
    }

    private void OnCombatStarted(CombatRoomController controller)
    {
        if (!ReferenceEquals(controller, _currentEncounterController))
            return;

        RequestStateChange(GameState.InCombat);
    }

    private void OnAnyCombatResolved(CombatRoomController controller, CombatRoomOutcome outcome)
    {
        if (!ReferenceEquals(controller, _currentEncounterController))
            return;

        if (outcome == CombatRoomOutcome.PlayerVictory)
        {
            TryRecordBossFloorShortcut(controller);
            RequestStateChange(GameState.CombatResolved);
            RequestStateChange(GameState.ExploringDungeon);
        }
        else
        {
            RequestStateChange(GameState.GameOver);
        }
    }

    /// <summary>
    /// If the resolved room is a Boss room and we are on a shortcut-eligible floor (multiple of 5),
    /// records the unlock in <see cref="ShortcutProgressService"/>.
    /// </summary>
    private void TryRecordBossFloorShortcut(CombatRoomController controller)
    {
        if (controller == null || controller.RoomProfile == null)
            return;

        if (controller.RoomProfile.RoomType != PDRoomType.Boss)
            return;

        if (ShortcutProgressService.Instance == null)
            return;

        PrefabDungeonGenerator generator = FindFirstObjectByType<PrefabDungeonGenerator>();
        if (generator == null)
        {
            Debug.LogWarning($"[{nameof(GameManager)}] Boss floor cleared but PrefabDungeonGenerator not found – shortcut not recorded.");
            return;
        }

        ShortcutProgressService.Instance.NotifyBossFloorCleared(generator.FloorNumber);
    }

    private void RefreshCurrentEncounterSubscription(CombatRoomController nextController)
    {
        if (ReferenceEquals(_currentEncounterController, nextController))
            return;

        ClearCurrentEncounterSubscription();
        _currentEncounterController = nextController;

        if (_currentEncounterController == null)
            return;

        _currentEncounterController.CombatStarted -= OnCombatStarted;
        _currentEncounterController.CombatStarted += OnCombatStarted;
    }

    private void ClearCurrentEncounterSubscription()
    {
        if (_currentEncounterController == null)
            return;

        _currentEncounterController.CombatStarted -= OnCombatStarted;
        _currentEncounterController = null;
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
