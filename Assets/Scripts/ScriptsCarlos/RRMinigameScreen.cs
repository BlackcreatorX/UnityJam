using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RRMinigameScreen : MonoBehaviour
{
    [SerializeField, Range(1, 6)] private int placeholderNumber = 4;
    [SerializeField] private bool usePlaceholderNumber = true;
    [SerializeField] private RRLuckyNumberManager numberManager;
    [SerializeField] private RRGameManager gameManager;
    [SerializeField] private RRouletteManager rouletteManager;
    [SerializeField] private RRClueManager clueManager;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI turnsText;
    [SerializeField] private TextMeshProUGUI cluesText;
    [SerializeField] private TextMeshProUGUI chamberText;
    [SerializeField] private Button[] gameplayButtons;
    private bool ended;

    private void Start()
    {
        if (numberManager == null || gameManager == null || statusText == null)
        {
            Debug.LogError("Minigame scene UI has not been generated yet. Run Tools > Minigame > Create Saved Canvas in the Unity Editor.");
            return;
        }
        if (usePlaceholderNumber) StartWithNumber(placeholderNumber);
        else statusText.text = "Waiting for the external manager to provide a number…";
        Refresh();
    }

    public bool StartWithExternalNumber(int number) => StartWithNumber(number);

    private bool StartWithNumber(int number)
    {
        if (numberManager == null || gameManager == null || !numberManager.SetLuckyNumber(number)) return false;
        gameManager.StartGame();
        Refresh();
        return true;
    }

    public void Guess1() => Guess(1);
    public void Guess2() => Guess(2);
    public void Guess3() => Guess(3);
    public void Guess4() => Guess(4);
    public void Guess5() => Guess(5);
    public void Guess6() => Guess(6);

    private void Guess(int value)
    {
        if (ended || !gameManager.CanPerformAction()) return;
        if (numberManager.CheckGuess(value))
        {
            gameManager.WinGame();
            statusText.text = "Correct! You win.";
        }
        else
        {
            statusText.text = "That is not the right number. Try again.";
            gameManager.UseAction();
        }
        Refresh();
    }

    public void RevealClue()
    {
        if (ended || !gameManager.CanPerformAction()) return;
        clueManager.RevealClue();
        gameManager.UseAction();
        Refresh();
    }

    public void Spin()
    {
        if (ended || !gameManager.CanPerformAction()) return;
        rouletteManager.SpinRoulette();
        chamberText.text = "Roulette spun: chamber " + (rouletteManager.CurrentChamber + 1) + "/" + rouletteManager.ChamberCount;
        gameManager.UseAction();
        Refresh();
    }

    public void Shoot()
    {
        if (ended) return;
        bool bulletFired = rouletteManager.PullTrigger();
        if (bulletFired)
        {
            chamberText.text = "BANG! The bullet was fired.";
            gameManager.PlayerShot(false);
        }
        else
        {
            chamberText.text = "Click. Empty chamber.";
            gameManager.PlayerShot(true);
        }
        Refresh();
    }

    private void Refresh()
    {
        if (turnsText == null || gameManager == null) return;
        turnsText.text = "Turns remaining: " + gameManager.RemainingTurns;
        var clues = clueManager.GetDiscoveredClues();
        cluesText.text = clues.Count == 0 ? "Clues: none" : "Clues:\n• " + string.Join("\n• ", clues);
        var state = gameManager.CurrentState;
        ended = state == RRGameManager.GameState.Victory || state == RRGameManager.GameState.Defeat;
        if (state == RRGameManager.GameState.ForcedShot) statusText.text = "Out of turns: you must pull the trigger.";
        if (state == RRGameManager.GameState.Defeat) statusText.text = "Game over: the roulette fired.";
        if (state == RRGameManager.GameState.Victory) statusText.text = "You win! You guessed the number.";
        if (gameplayButtons != null) foreach (var button in gameplayButtons) if (button != null) button.interactable = !ended;
    }
}
