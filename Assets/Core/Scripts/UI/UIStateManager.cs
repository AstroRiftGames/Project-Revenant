using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestHideUI(UIType.Minimap);
            GameManager.Instance.RequestHideUI(UIType.LOG);
            GameManager.Instance.RequestHideUI(UIType.DeepInspector);
            GameManager.Instance.RequestHideUI(UIType.CombatStatus);
            
            if (GameManager.Instance.StateManager != null)
            {
                GameManager.Instance.StateManager.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameState.MainMenu, GameManager.Instance.StateManager.CurrentState);
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.StateManager != null)
        {
            GameManager.Instance.StateManager.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        if (GameManager.Instance == null) return;

        switch (next)
        {
            case GameState.SafeZone:
                GameManager.Instance.RequestHideUI(UIType.Inspector);
                GameManager.Instance.RequestHideUI(UIType.Minimap);
                GameManager.Instance.RequestHideUI(UIType.DeepInspector);
                GameManager.Instance.RequestHideUI(UIType.LOG);
                GameManager.Instance.RequestHideUI(UIType.CombatStatus);
                break;

            case GameState.ExploringDungeon:
            case GameState.StationUI: // Station handles its own UI, but base elements remain the same
            case GameState.Deployment:
                GameManager.Instance.RequestShowUI(UIType.Inspector);
                GameManager.Instance.RequestShowUI(UIType.Minimap);
                GameManager.Instance.RequestHideUI(UIType.LOG);
                GameManager.Instance.RequestHideUI(UIType.CombatStatus);
                break;

            case GameState.InCombat:
                GameManager.Instance.RequestShowUI(UIType.Inspector);
                GameManager.Instance.RequestHideUI(UIType.Minimap);
                GameManager.Instance.RequestShowUI(UIType.LOG);
                GameManager.Instance.RequestShowUI(UIType.CombatStatus);
                break;

            case GameState.GameOver:
                GameManager.Instance.RequestHideUI(UIType.Minimap);
                GameManager.Instance.RequestHideUI(UIType.LOG);
                GameManager.Instance.RequestHideUI(UIType.CombatStatus);
                break;
        }
    }
}
