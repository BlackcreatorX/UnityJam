using UnityEngine;
using UnityEngine.Events;
public class RRouletteManager : MonoBehaviour
{[Header("Roulette Settings")]
    [SerializeField] private int chamberCount = 6;

    [Header("Events")]
    public UnityEvent OnBulletFired;
    public UnityEvent OnEmptyShot;
    public UnityEvent<int> OnChamberChanged;

    // Internal roulette data
    private int bulletChamber;
    private int currentChamber;

    public int CurrentChamber => currentChamber;
    public int ChamberCount => chamberCount;

    public bool IsLoaded { get; private set; }

    public void InitializeRoulette()
    {
        // Random chamber containing the bullet
        bulletChamber = Random.Range(0, chamberCount);

        // Initial chamber
        currentChamber = 0;

        IsLoaded = true;

        Debug.Log("Roulette initialized.");
    }

    public void SpinRoulette()
    {
        if (!IsLoaded)
            return;

        // Randomize the current chamber
        currentChamber = Random.Range(0, chamberCount);

        OnChamberChanged?.Invoke(currentChamber);

        Debug.Log("Roulette spun.");
    }

    public bool PullTrigger()
    {
        if (!IsLoaded)
            return false;

        bool bulletFired = currentChamber == bulletChamber;

        if (bulletFired)
        {
            IsLoaded = false;

            OnBulletFired?.Invoke();

            Debug.Log("BANG! Bullet fired.");
            return true;
        }

        // Move to next chamber
        currentChamber = (currentChamber + 1) % chamberCount;

        OnEmptyShot?.Invoke();
        OnChamberChanged?.Invoke(currentChamber);

        Debug.Log("Click! Empty chamber.");

        return false;
    }

    public void ReloadRoulette()
    {
        InitializeRoulette();
    }
}
