using UnityEngine;
using UnityEngine.SceneManagement;
using PrefabDungeonGeneration;

public class UIStateManager : MonoBehaviour
{
    private CombatRoomController _currentCombatRoom;

    private void Start()
    {
        // Initial setup as requested
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestHideUI(UIType.Minimap);
            GameManager.Instance.RequestHideUI(UIType.ProgressBar);
            GameManager.Instance.RequestHideUI(UIType.LOG);
        }

        CheckCurrentScene(SceneManager.GetActiveScene());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FloorManager.OnRoomEntered += OnRoomEntered;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FloorManager.OnRoomEntered -= OnRoomEntered;
        UnsubscribeFromCombatRoom();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckCurrentScene(scene);
    }

    private void CheckCurrentScene(Scene scene)
    {
        if (GameManager.Instance == null) return;

        // Inspector: Oculto durante la SafeZone, siempre mostrado durante la Dungeon.
        // Minimap: Se muestra al entrar a la dungeon (Start lo inicia oculto).
        if (scene.name == "SafeZone")
        {
            GameManager.Instance.RequestHideUI(UIType.Inspector);
            GameManager.Instance.RequestHideUI(UIType.Minimap);
        }
        else if (scene.name == "Dungeon")
        {
            GameManager.Instance.RequestShowUI(UIType.Inspector);
            GameManager.Instance.RequestShowUI(UIType.Minimap);
        }
    }

    private void OnRoomEntered(RoomDoor door, GameObject nextRoom)
    {
        UnsubscribeFromCombatRoom();

        if (nextRoom != null && nextRoom.TryGetComponent(out RoomContext roomContext))
        {
            if (roomContext.IsCombatRoom && roomContext.CombatController != null)
            {
                _currentCombatRoom = roomContext.CombatController;
                _currentCombatRoom.CombatStarted += OnCombatStarted;
                _currentCombatRoom.CombatResolved += OnCombatResolved;

                // Si ya está en combate por alguna razón, sincronizar el estado
                if (_currentCombatRoom.IsCombatActive)
                {
                    OnCombatStarted(_currentCombatRoom);
                }
                else if (_currentCombatRoom.IsResolved)
                {
                    OnCombatResolved(_currentCombatRoom, _currentCombatRoom.Outcome);
                }
            }
            else
            {
                // Si entra a una room normal, asegurar que el Minimap esté visible y el LOG oculto
                if (GameManager.Instance != null && SceneManager.GetActiveScene().name == "Dungeon")
                {
                    GameManager.Instance.RequestShowUI(UIType.Minimap);
                    GameManager.Instance.RequestHideUI(UIType.LOG);
                }
            }
        }
    }

    private void OnCombatStarted(CombatRoomController controller)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestHideUI(UIType.Minimap);
            GameManager.Instance.RequestShowUI(UIType.LOG);
        }
    }

    private void OnCombatResolved(CombatRoomController controller, CombatRoomOutcome outcome)
    {
        if (GameManager.Instance != null && SceneManager.GetActiveScene().name == "Dungeon")
        {
            GameManager.Instance.RequestShowUI(UIType.Minimap);
            GameManager.Instance.RequestHideUI(UIType.LOG);
        }
    }

    private void UnsubscribeFromCombatRoom()
    {
        if (_currentCombatRoom != null)
        {
            _currentCombatRoom.CombatStarted -= OnCombatStarted;
            _currentCombatRoom.CombatResolved -= OnCombatResolved;
            _currentCombatRoom = null;
        }
    }
}
