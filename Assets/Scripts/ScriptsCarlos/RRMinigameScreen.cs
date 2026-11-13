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
        else statusText.text = "Esperando el número del manager externo…";
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
            statusText.text = "¡Correcto! Ganaste.";
        }
        else
        {
            statusText.text = "No era ese número. Intenta otra vez.";
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
        chamberText.text = "Ruleta girada: cámara " + (rouletteManager.CurrentChamber + 1) + "/" + rouletteManager.ChamberCount;
        gameManager.UseAction();
        Refresh();
    }

    public void Shoot()
    {
        if (ended) return;
        bool bulletFired = rouletteManager.PullTrigger();
        if (bulletFired)
        {
            chamberText.text = "¡BANG! La ruleta alcanzó la bala.";
            gameManager.PlayerShot(false);
        }
        else
        {
            chamberText.text = "Click. Cámara vacía.";
            gameManager.PlayerShot(true);
        }
        Refresh();
    }

    private void Refresh()
    {
        if (turnsText == null || gameManager == null) return;
        turnsText.text = "Turnos restantes: " + gameManager.RemainingTurns;
        var clues = clueManager.GetDiscoveredClues();
        cluesText.text = clues.Count == 0 ? "Pistas: ninguna" : "Pistas:\n• " + string.Join("\n• ", clues);
        var state = gameManager.CurrentState;
        ended = state == RRGameManager.GameState.Victory || state == RRGameManager.GameState.Defeat;
        if (state == RRGameManager.GameState.ForcedShot) statusText.text = "Te quedaste sin turnos: debes jalar el gatillo.";
        if (state == RRGameManager.GameState.Defeat) statusText.text = "Fin de la partida: la ruleta disparó.";
        if (state == RRGameManager.GameState.Victory) statusText.text = "¡Ganaste! Adivinaste el número.";
        if (gameplayButtons != null) foreach (var button in gameplayButtons) if (button != null) button.interactable = !ended;
    }
}
