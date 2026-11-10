using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class HorseRaceMinigame : MonoBehaviour
{
    [Header("Caballos (5, de arriba hacia abajo)")]
    public Horse[] horses = new Horse[5];
    public int playerHorseIndex = 0;

    [Header("Pista")]
    public Transform startLine;
    public Transform finishLine;
    public int stepsToWin = 15;

    [Header("Tablero de tiro")]
    public AimBallBoard board;
    public float shotMessageTime = 0.7f;

    [Header("Rivales (IA)")]
    public float aiTickMin = 1.4f;
    public float aiTickMax = 2.1f;
    public int[] aiPointWeights = { 2, 4, 3, 1 };

    [Header("Premio")]
    public int goldOnWin = 50;
    public int goldOnLose = 5;

    [Header("Número de la suerte")]
    [Range(1, 6)] public int luckyNumber = 1;

    [Header("Pista en la carrera")]
    public float hintFlashDuration = 1.8f;
    [Range(0f, 1f)] public float hintMinProgress = 0.3f;
    [Range(0f, 1f)] public float hintMaxProgress = 0.7f;
    public bool pauseDuringHint = true;
    public SpriteRenderer flashOverlay;
    public float flashAlpha = 0.6f;
    public float flashFade = 0.3f;

    [Header("Flujo")]
    public bool autoStart = true;

    [Header("UI")]
    public TMP_Text messageText;
    public GameObject countdownRoot;
    public TMP_Text countdownText;
    public float countdownStep = 0.7f;
    public float countdownPop = 1.5f;
    public float countdownPopTime = 0.2f;

    [Header("Eventos")]
    public UnityEvent<int> onGoldEarned;
    public UnityEvent<int> onLuckyHorseRevealed;
    public UnityEvent<bool> onRaceFinished;

    bool raceOver = true;
    bool paused;
    int messageId;

    int[] horseNumbers;
    int luckyHorse;
    int hintAtStep;
    bool hintShown;

    Horse Player => horses[playerHorseIndex];

    public void Setup(int number)
    {
        luckyNumber = Mathf.Clamp(number, 1, 6);
    }

    public void StartRace()
    {
        StopAllCoroutines();
        StartCoroutine(RaceRoutine());
    }

    void OnEnable()
    {
        if (board) board.Resolved += OnShot;
    }

    void OnDisable()
    {
        if (board) board.Resolved -= OnShot;
    }

    void Start()
    {
        if (autoStart) StartRace();
    }

    IEnumerator RaceRoutine()
    {
        raceOver = false;
        paused = false;
        board.inputEnabled = false;
        board.paused = false;
        board.ResetBall();
        SetFlashAlpha(0f);
        Show("");
        if (countdownRoot) countdownRoot.SetActive(false);

        float stepSize = (finishLine.position.x - startLine.position.x) / stepsToWin;
        foreach (var h in horses) h.Init(startLine.position.x, stepSize);

        PrepareHint();

        for (int i = 3; i > 0; i--)
            yield return Countdown(i.ToString(), countdownStep);

        board.inputEnabled = true;
        for (int i = 0; i < horses.Length; i++)
            if (i != playerHorseIndex) StartCoroutine(AiRoutine(horses[i]));

        yield return Countdown("¡ARRANCAN!", countdownStep + 0.2f);
        if (countdownRoot) countdownRoot.SetActive(false);
    }

    IEnumerator Countdown(string text, float duration)
    {
        if (!countdownRoot || !countdownText)
        {
            Show(text);
            yield return new WaitForSeconds(duration);
            yield break;
        }

        countdownRoot.SetActive(true);
        countdownText.text = text;

        var tr = countdownText.transform;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / countdownPopTime);
            tr.localScale = Vector3.one * Mathf.Lerp(countdownPop, 1f, k);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    void OnShot(int pts)
    {
        if (raceOver) return;
        ShowTemp(pts > 0 ? $"+{pts}" : "¡Fallaste!", shotMessageTime);
        Move(Player, pts);
    }

    IEnumerator AiRoutine(Horse h)
    {
        while (!raceOver)
        {
            yield return new WaitForSeconds(Random.Range(aiTickMin, aiTickMax));
            while (paused && !raceOver) yield return null;
            Move(h, WeightedPick(aiPointWeights));
        }
    }

    void Move(Horse h, int pts)
    {
        if (raceOver) return;
        h.Advance(pts, stepsToWin);

        if (!hintShown && LeaderSteps() >= hintAtStep)
        {
            hintShown = true;
            StartCoroutine(FlashHint());
        }

        if (h.Steps >= stepsToWin) EndRace(h);
    }

    void EndRace(Horse winner)
    {
        raceOver = true;
        paused = false;
        board.inputEnabled = false;
        board.paused = false;
        StartCoroutine(EndRoutine(winner));
    }

    IEnumerator EndRoutine(Horse winner)
    {
        while (winner.IsMoving) yield return null;

        bool playerWon = winner == Player;

        if (playerWon)
        {
            int lane = horses[luckyHorse].laneNumber;
            string who = luckyHorse == playerHorseIndex ? "tu caballo" : $"el caballo #{lane}";
            Show($"¡GANASTE! +{goldOnWin} oro\nEl número de la suerte lo traía {who}");
            onGoldEarned?.Invoke(goldOnWin);
            onLuckyHorseRevealed?.Invoke(lane);
        }
        else
        {
            Show($"Ganó el caballo #{winner.laneNumber}\n+{goldOnLose} oro");
            if (goldOnLose > 0) onGoldEarned?.Invoke(goldOnLose);
        }

        onRaceFinished?.Invoke(playerWon);
    }

    void PrepareHint()
    {
        var others = new List<int>();
        for (int n = 1; n <= 6; n++)
            if (n != luckyNumber) others.Add(n);

        for (int i = others.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (others[i], others[j]) = (others[j], others[i]);
        }

        horseNumbers = new int[horses.Length];
        luckyHorse = Random.Range(0, horses.Length);

        int k = 0;
        for (int i = 0; i < horses.Length; i++)
            horseNumbers[i] = i == luckyHorse ? luckyNumber : others[k++];

        float progress = Random.Range(hintMinProgress, hintMaxProgress);
        hintAtStep = Mathf.Clamp(Mathf.RoundToInt(stepsToWin * progress), 1, stepsToWin - 1);
        hintShown = false;
    }

    IEnumerator FlashHint()
    {
        if (pauseDuringHint)
        {
            paused = true;
            board.paused = true;
        }

        for (int i = 0; i < horses.Length; i++)
            horses[i].ShowDorsal(horseNumbers[i].ToString());

        float t = 0f;
        while (t < hintFlashDuration)
        {
            t += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(flashAlpha, 0f, t / flashFade));
            yield return null;
        }

        SetFlashAlpha(0f);
        foreach (var h in horses) h.HideDorsal();

        paused = false;
        board.paused = false;
    }

    void SetFlashAlpha(float a)
    {
        if (!flashOverlay) return;
        var c = flashOverlay.color;
        c.a = a;
        flashOverlay.color = c;
    }

    int LeaderSteps()
    {
        int max = 0;
        foreach (var h in horses) if (h.Steps > max) max = h.Steps;
        return max;
    }

    static int WeightedPick(int[] weights)
    {
        int total = 0;
        foreach (int w in weights) total += w;
        int r = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (r < weights[i]) return i;
            r -= weights[i];
        }
        return 0;
    }

    void Show(string msg)
    {
        messageId++;
        if (messageText) messageText.text = msg;
    }

    void ShowTemp(string msg, float time)
    {
        Show(msg);
        StartCoroutine(ClearAfter(messageId, time));
    }

    IEnumerator ClearAfter(int id, float time)
    {
        yield return new WaitForSeconds(time);
        if (id == messageId && !raceOver) Show("");
    }
}
