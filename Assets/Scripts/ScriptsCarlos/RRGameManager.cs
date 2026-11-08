
using UnityEngine;
using UnityEngine.Events;

public class RRGameManager : MonoBehaviour
{
    public enum GameState
    {
        Starting,
        PlayerTurn,
        ForcedShot,
        Victory,
        Defeat
    }

    [Header("Game Settings")]
    [SerializeField] private int maxTurns = 5;

    [Header("References")]
    [SerializeField] private RRLuckyNumberManager luckyNumberManager;
    [SerializeField] private RRouletteManager rouletteManager;
    [SerializeField] private RRClueManager clueManager;

    [Header("Events")]
    public UnityEvent<int> OnTurnChanged;
    public UnityEvent<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }

    public int CurrentTurn { get; private set; }
    public int RemainingTurns { get; private set; }
    private bool hasStarted;

    public void Configure(RRLuckyNumberManager luckyNumber, RRouletteManager roulette, RRClueManager clues)
    {
        luckyNumberManager = luckyNumber;
        rouletteManager = roulette;
        clueManager = clues;
    }

    public void StartGame()
    {
        if (hasStarted)
        {
            Debug.LogWarning("This minigame session has already started and cannot be replayed.");
            return;
        }

        if (luckyNumberManager == null || !luckyNumberManager.HasLuckyNumber)
        {
            Debug.LogError("Set the player target with RRLuckyNumberManager.SetLuckyNumber before starting the minigame.");
            return;
        }

        hasStarted = true;
        CurrentTurn = 0;
        RemainingTurns = maxTurns;

        SetState(GameState.Starting);

        if (rouletteManager != null)
            rouletteManager.InitializeRoulette();

        if (clueManager != null)
            clueManager.InitializeClues();

        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        if (RemainingTurns <= 0)
        {
            SetState(GameState.ForcedShot);
            return;
        }

        CurrentTurn++;

        SetState(GameState.PlayerTurn);

        OnTurnChanged?.Invoke(RemainingTurns);
    }

    public void UseAction()
    {
        if (CurrentState != GameState.PlayerTurn)
            return;

        RemainingTurns--;

        OnTurnChanged?.Invoke(RemainingTurns);

        if (RemainingTurns <= 0)
        {
            SetState(GameState.ForcedShot);
        }
    }

    public void EndTurn()
    {
        if (CurrentState != GameState.PlayerTurn)
            return;

        StartPlayerTurn();
    }

    public void PlayerShot(bool survived)
    {
        if (CurrentState != GameState.PlayerTurn &&
            CurrentState != GameState.ForcedShot)
            return;

        if (!survived)
        {
            SetState(GameState.Defeat);
            return;
        }

        if (CurrentState == GameState.ForcedShot)
        {
            RemainingTurns = maxTurns;
            StartPlayerTurn();
        }
        else
        {
            EndTurn();
        }
    }

    public void WinGame()
    {
        if (CurrentState == GameState.Victory ||
            CurrentState == GameState.Defeat)
            return;

        SetState(GameState.Victory);
    }

    public void LoseGame()
    {
        SetState(GameState.Defeat);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);

        Debug.Log("Game State: " + CurrentState);
    }

    public bool CanPerformAction()
    {
        return CurrentState == GameState.PlayerTurn;
    }
}
