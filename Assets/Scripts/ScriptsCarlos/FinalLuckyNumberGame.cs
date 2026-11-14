using System.Collections;
using DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FinalLuckyNumberGame : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence victoryDialogue;
    [SerializeField] private DialogueSequence defeatDialogue;
    [SerializeField] private GameObject challengeCanvas;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button[] numberButtons;

    private int[] luckyNumbers;
    private readonly int[] enteredNumbers = new int[4];
    private int enteredCount;
    private bool numbersAvailable;
    private bool challengeVisible;
    private bool resultDialogueActive;
    private bool playerWon;
    private bool completed;

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    private void Start()
    {
        numbersAvailable = LuckyNumberProgress.TryGetAll(out luckyNumbers);
        if (challengeCanvas != null) challengeCanvas.SetActive(false);
        StartCoroutine(CheckInitialDialogue());
    }

    private IEnumerator CheckInitialDialogue()
    {
        yield return null;
        if (dialogueManager == null || !dialogueManager.IsDialogueActive)
            ShowChallenge();
    }

    private void HandleDialogueEnded()
    {
        if (resultDialogueActive)
        {
            resultDialogueActive = false;
            SceneManager.LoadScene("MainMenu");
            return;
        }

        ShowChallenge();
    }

    private void ShowChallenge()
    {
        if (challengeVisible) return;
        challengeVisible = true;
        if (challengeCanvas != null) challengeCanvas.SetActive(true);

        if (numbersAvailable)
        {
            SetButtonsInteractable(true);
            SetStatus("Enter number 1 of 4.");
        }
        else
        {
            SetStatus("Lucky numbers are missing. Complete all four previous minigames first.");
            SetButtonsInteractable(false);
        }
    }

    public void SubmitNumber(int number)
    {
        if (completed || !numbersAvailable || enteredCount >= enteredNumbers.Length)
            return;

        enteredNumbers[enteredCount++] = number;
        if (enteredCount < enteredNumbers.Length)
        {
            SetStatus($"Number {enteredCount} entered. Choose number {enteredCount + 1} of 4.");
            return;
        }

        playerWon = true;
        for (int index = 0; index < luckyNumbers.Length; index++)
        {
            if (enteredNumbers[index] == luckyNumbers[index]) continue;
            playerWon = false;
            break;
        }

        completed = true;
        SetButtonsInteractable(false);
        if (challengeCanvas != null) challengeCanvas.SetActive(false);

        DialogueSequence result = playerWon ? victoryDialogue : defeatDialogue;
        if (dialogueManager != null && result != null)
        {
            resultDialogueActive = true;
            dialogueManager.StartDialogue(result);
        }
        else
        {
            if (challengeCanvas != null) challengeCanvas.SetActive(true);
            SetStatus(playerWon ? "You remembered all four lucky numbers!" : "The numbers did not match.");
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (numberButtons == null) return;
        foreach (Button button in numberButtons)
            if (button != null) button.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
