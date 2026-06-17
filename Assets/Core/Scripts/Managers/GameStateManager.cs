using UnityEngine;
using System;

public class GameStateManager : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    public event Action<GameState, GameState> OnStateChanged;

    public bool TryTransitionTo(GameState nextState)
    {
        if (CurrentState == nextState)
            return true;

        if (!IsValidTransition(CurrentState, nextState))
        {
            Debug.LogWarning($"[GameStateManager] Invalid transition requested from {CurrentState} to {nextState}. Ignored.");
            return false;
        }

        GameState previousState = CurrentState;
        CurrentState = nextState;
        OnStateChanged?.Invoke(previousState, nextState);
        return true;
    }

    private bool IsValidTransition(GameState previous, GameState next)
    {
        // Any state can transition to MainMenu (e.g. for hard resets)
        if (next == GameState.MainMenu)
            return true;

        switch (previous)
        {
            case GameState.MainMenu:
                return next == GameState.SafeZone || next == GameState.Paused;

            case GameState.SafeZone:
                return next == GameState.ExploringDungeon || next == GameState.StationUI || next == GameState.Dialogue || next == GameState.Paused;

            case GameState.ExploringDungeon:
                return next == GameState.Deployment || next == GameState.StationUI || next == GameState.Dialogue || next == GameState.Paused;

            case GameState.Deployment:
                return next == GameState.InCombat || next == GameState.ExploringDungeon || next == GameState.Paused;

            case GameState.InCombat:
                return next == GameState.CombatResolved || next == GameState.GameOver || next == GameState.Paused;

            case GameState.CombatResolved:
                return next == GameState.ExploringDungeon || next == GameState.Paused;

            case GameState.StationUI:
                return next == GameState.SafeZone || next == GameState.ExploringDungeon || next == GameState.Paused;

            case GameState.GameOver:
                return next == GameState.SafeZone || next == GameState.Paused;

            case GameState.Dialogue:
                return next == GameState.SafeZone || next == GameState.ExploringDungeon || next == GameState.StationUI || next == GameState.Paused;

            case GameState.Paused:
                return next == GameState.SafeZone || next == GameState.ExploringDungeon || next == GameState.Deployment || next == GameState.InCombat || next == GameState.CombatResolved || next == GameState.StationUI || next == GameState.Dialogue;

            default:
                return false;
        }
    }
}
