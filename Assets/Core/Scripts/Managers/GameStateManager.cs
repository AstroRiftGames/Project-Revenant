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
                return next == GameState.SafeZone;

            case GameState.SafeZone:
                return next == GameState.ExploringDungeon || next == GameState.StationUI;

            case GameState.ExploringDungeon:
                return next == GameState.Deployment || next == GameState.StationUI;

            case GameState.Deployment:
                return next == GameState.InCombat || next == GameState.ExploringDungeon;

            case GameState.InCombat:
                return next == GameState.CombatResolved || next == GameState.GameOver;

            case GameState.CombatResolved:
                return next == GameState.ExploringDungeon;

            case GameState.StationUI:
                return next == GameState.SafeZone || next == GameState.ExploringDungeon;

            case GameState.GameOver:
                return next == GameState.SafeZone;

            default:
                return false;
        }
    }
}
