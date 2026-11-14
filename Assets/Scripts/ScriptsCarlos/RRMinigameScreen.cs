using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DialogueSystem;
using UnityEngine.SceneManagement;

public class RRMinigameScreen : MonoBehaviour
{
    [SerializeField, Range(1, 6)] private int placeholderNumber = 4;
    [SerializeField] private bool usePlaceholderNumber = true;
    [SerializeField] private RRLuckyNumberManager numberManager;
    [SerializeField] private RRGameManager gameManager;
    [SerializeField] private RRouletteManager rouletteManager;
    [SerializeField] private RRClueManager clueManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence victoryDialogue;
    [SerializeField] private DialogueSequence defeatDialogue;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI turnsText;
    [SerializeField] private TextMeshProUGUI cluesText;
    [SerializeField] private TextMeshProUGUI chamberText;
    [SerializeField] private Button[] gameplayButtons;
    private bool ended;
    private bool openingDialogueEnded;
    private bool pendingStart;
    private bool gameStarted;
    private bool resultDialogueActive;
    private bool pendingVictory;

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += HandleDialogueEnded;
        if (gameManager != null)
            gameManager.OnGameStateChanged.AddListener(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
        if (gameManager != null)
            gameManager.OnGameStateChanged.RemoveListener(HandleGameStateChanged);
    }

    private void Start()
    {
        if (numberManager == null || gameManager == null || statusText == null)
        {
            Debug.LogError("Minigame scene UI has not been generated yet. Run Tools > Minigame > Create Saved Canvas in the Unity Editor.");
            return;
        }
        if (gameplayRoot != null) gameplayRoot.SetActive(false);
        if (usePlaceholderNumber) StartWithNumber(placeholderNumber);
        else statusText.text = "Waiting for the external manager to provide a number…";
        StartCoroutine(CheckOpeningDialogue());
    }

    private System.Collections.IEnumerator CheckOpeningDialogue()
    {
        // DialogueManager starts its initial sequence in Start; wait until all Start methods have run.
        yield return null;
        if (dialogueManager == null || !dialogueManager.IsDialogueActive)
        {
            openingDialogueEnded = true;
            TryStartGame();
        }
    }

    private void HandleDialogueEnded()
    {
        if (resultDialogueActive)
        {
            resultDialogueActive = false;
            FinishOutcome(pendingVictory);
            return;
        }

        openingDialogueEnded = true;
        TryStartGame();
    }

    public bool StartWithExternalNumber(int number) => StartWithNumber(number);

    private bool StartWithNumber(int number)
    {
        if (numberManager == null || gameManager == null || !numberManager.SetLuckyNumber(number)) return false;
        LuckyNumberProgress.RecordCharlie(number);
        pendingStart = true;
        TryStartGame();
        return true;
    }

    private void TryStartGame()
    {
        if (!openingDialogueEnded || !pendingStart || gameStarted) return;

        gameStarted = true;
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
        gameManager.StartGame();
        Refresh();
    }

    private void HandleGameStateChanged(RRGameManager.GameState state)
    {
        if (state != RRGameManager.GameState.Victory && state != RRGameManager.GameState.Defeat)
            return;

        ended = true;
        if (gameplayButtons != null)
            foreach (var button in gameplayButtons)
                if (button != null) button.interactable = false;

        pendingVictory = state == RRGameManager.GameState.Victory;
        DialogueSequence sequence = pendingVictory ? victoryDialogue : defeatDialogue;
        if (dialogueManager != null && sequence != null)
        {
            resultDialogueActive = true;
            dialogueManager.StartDialogue(sequence);
        }
        else
        {
            FinishOutcome(pendingVictory);
        }
    }

    private void FinishOutcome(bool victory)
    {
        if (victory)
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex >= 0 && nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadScene(nextSceneIndex);
            else
                SceneManager.LoadScene("MainMenu");
            return;
        }

        ShowGameOver();
    }

    private void ShowGameOver()
    {
        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

        GameObject labelObject = new GameObject("GameOverText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.1f, 0.25f);
        labelRect.anchorMax = new Vector2(0.9f, 0.75f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = statusText.font;
        label.text = "GAME OVER\n\nThe roulette fired. Mammon wins.";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 56f;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 28f;
        label.fontSizeMax = 56f;
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
