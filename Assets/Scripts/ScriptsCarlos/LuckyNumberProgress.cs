using UnityEngine;

/// <summary>Stores the four lucky numbers in the order used by the final challenge.</summary>
public static class LuckyNumberProgress
{
    private const string RouletteKey = "FinalGame.LuckyNumber.Roulette";
    private const string HorseRaceKey = "FinalGame.LuckyNumber.HorseRace";
    private const string CharlieKey = "FinalGame.LuckyNumber.Charlie";
    private const string CardsKey = "FinalGame.LuckyNumber.Cards";

    public static void RecordRoulette(int number) => Record(RouletteKey, number);
    public static void RecordHorseRace(int number) => Record(HorseRaceKey, number);
    public static void RecordCharlie(int number) => Record(CharlieKey, number);
    public static void RecordCards(int number) => Record(CardsKey, number);

    public static bool TryGetAll(out int[] numbers)
    {
        numbers = new[]
        {
            PlayerPrefs.GetInt(RouletteKey, 0),
            PlayerPrefs.GetInt(HorseRaceKey, 0),
            PlayerPrefs.GetInt(CharlieKey, 0),
            PlayerPrefs.GetInt(CardsKey, 0)
        };

        foreach (int number in numbers)
            if (number < 1 || number > 6)
                return false;

        return true;
    }

    private static void Record(string key, int number)
    {
        if (number < 1 || number > 6)
            return;

        PlayerPrefs.SetInt(key, number);
        PlayerPrefs.Save();
    }
}
