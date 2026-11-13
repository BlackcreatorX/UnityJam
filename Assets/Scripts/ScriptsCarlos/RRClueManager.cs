
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class RRClueManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RRLuckyNumberManager luckyNumberManager;

    [Header("Clue Settings")]
    [SerializeField] private int maxClues = 5;

    [Header("Events")]
    public UnityEvent<string> OnClueGenerated;
    public UnityEvent OnNoMoreClues;

    private List<string> availableClues = new List<string>();
    private List<string> discoveredClues = new List<string>();
    private int totalRevealedClues;

    public int DiscoveredClueCount => discoveredClues.Count;
    public bool HasAvailableClues => availableClues.Count > 0;

    public void Configure(RRLuckyNumberManager manager)
    {
        luckyNumberManager = manager;
    }

    public void InitializeClues()
    {
        availableClues.Clear();
        discoveredClues.Clear();
        totalRevealedClues = 0;

        if (luckyNumberManager == null)
        {
            Debug.LogError("LuckyNumberManager reference missing!");
            return;
        }

        GenerateCluePool();
    }

    private void GenerateCluePool()
    {
        // These clues are always valid because
        // they are generated using the secret number.

        if (luckyNumberManager.IsNumberEven())
            availableClues.Add("Your lucky number is even.");
        else
            availableClues.Add("Your lucky number is odd.");

        if (luckyNumberManager.IsNumberGreaterThan(3))
            availableClues.Add("Your lucky number is greater than 3.");
        else
            availableClues.Add("Your lucky number is 3 or less.");

        if (luckyNumberManager.IsNumberGreaterThan(1))
            availableClues.Add("Your lucky number is greater than 1.");

        if (luckyNumberManager.IsNumberLessThan(6))
            availableClues.Add("Your lucky number is less than 6.");

        // Exact distance clues
        int secret = GetSecretNumberForClues();

        if (secret > 1)
            availableClues.Add("Your lucky number is close to " + (secret - 1) + ".");

        if (secret < 6)
            availableClues.Add("Your lucky number is close to " + (secret + 1) + ".");

        ShuffleClues();
    }

    private int GetSecretNumberForClues()
    {
        // We use the public clue comparison methods
        // to reconstruct the secret number.
        for (int i = 1; i <= 6; i++)
        {
            bool even = luckyNumberManager.IsNumberEven() == (i % 2 == 0);

            bool greater = luckyNumberManager.IsNumberGreaterThan(i);
            bool less = luckyNumberManager.IsNumberLessThan(i);

            if (even && !greater && !less)
                return i;
        }

        return -1;
    }

    private void ShuffleClues()
    {
        for (int i = 0; i < availableClues.Count; i++)
        {
            int randomIndex = Random.Range(i, availableClues.Count);

            string temp = availableClues[i];
            availableClues[i] = availableClues[randomIndex];
            availableClues[randomIndex] = temp;
        }
    }

    public void RevealClue()
    {
        if (totalRevealedClues >= maxClues ||
            availableClues.Count == 0)
        {
            OnNoMoreClues?.Invoke();
            return;
        }

        string clue = availableClues[0];

        availableClues.RemoveAt(0);
        totalRevealedClues++;
        if (discoveredClues.Count >= 2)
            discoveredClues.RemoveAt(0);
        discoveredClues.Add(clue);

        OnClueGenerated?.Invoke(clue);

        Debug.Log("New clue: " + clue);
    }

    public List<string> GetDiscoveredClues()
    {
        return new List<string>(discoveredClues);
    }
}
