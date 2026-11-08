using UnityEngine;
using UnityEngine.Events;
public class RRLuckyNumberManager : MonoBehaviour
{
   [Header("Lucky Number Settings")]
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumber = 6;

    [Header("Events")]
    public UnityEvent<int> OnNumberGuessed;
    public UnityEvent OnCorrectGuess;
    public UnityEvent OnIncorrectGuess;

    // The secret number is never exposed directly to the UI.
    private int luckyNumber;

    public bool IsNumberDiscovered { get; private set; }
    public bool HasLuckyNumber { get; private set; }

    /// <summary>Sets the number supplied by the external game manager.</summary>
    public bool SetLuckyNumber(int number)
    {
        if (number < minNumber || number > maxNumber)
        {
            Debug.LogError($"Lucky number {number} is outside the allowed range ({minNumber}-{maxNumber}).");
            return false;
        }

        if (HasLuckyNumber)
        {
            Debug.LogWarning("Lucky number is already set; this minigame can only be initialized once.");
            return false;
        }

        luckyNumber = number;
        HasLuckyNumber = true;
        IsNumberDiscovered = false;
        Debug.Log("Lucky Number received from external manager.");
        return true;
    }

    public bool CheckGuess(int playerGuess)
    {
        if (!HasLuckyNumber || IsNumberDiscovered)
            return false;

        bool isCorrect = playerGuess == luckyNumber;

        OnNumberGuessed?.Invoke(playerGuess);

        if (isCorrect)
        {
            IsNumberDiscovered = true;

            OnCorrectGuess?.Invoke();

            Debug.Log("Lucky Number discovered!");
        }
        else
        {
            OnIncorrectGuess?.Invoke();

            Debug.Log("Wrong number. Try again!");
        }

        return isCorrect;
    }

    public bool IsNumberGreaterThan(int value)
    {
        return luckyNumber > value;
    }

    public bool IsNumberLessThan(int value)
    {
        return luckyNumber < value;
    }

    public bool IsNumberEven()
    {
        return luckyNumber % 2 == 0;
    }

    public bool IsNumberOdd()
    {
        return luckyNumber % 2 != 0;
    }

    public bool IsWithinRange(int min, int max)
    {
        return luckyNumber >= min && luckyNumber <= max;
    }
}



