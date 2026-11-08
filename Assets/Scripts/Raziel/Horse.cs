using System.Collections;
using TMPro;
using UnityEngine;

public class Horse : MonoBehaviour
{
    public int laneNumber = 1;

    [Header("Salto al avanzar")]
    public float hopHeight = 0.25f;
    public float hopDuration = 0.15f;

    [Header("Dorsal de la pista")]
    public GameObject dorsal;
    public TMP_Text dorsalText;
    public Vector3 dorsalOffset = new Vector3(0f, 0.65f, 0f);

    public int Steps { get; private set; }
    public bool IsMoving => moveRoutine != null;

    Vector3 startPos;
    float stepSize;
    Coroutine moveRoutine;

    public void Init(float startX, float stepSize)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;

        this.stepSize = stepSize;
        startPos = new Vector3(startX, transform.position.y, transform.position.z);
        transform.position = startPos;
        Steps = 0;

        HideDorsal();
    }

    public void Advance(int amount, int maxSteps)
    {
        if (amount <= 0) return;

        Steps = Mathf.Min(Steps + amount, maxSteps);
        Vector3 target = startPos + Vector3.right * stepSize * Steps;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(HopTo(target));
    }

    public void ShowDorsal(string text)
    {
        if (dorsalText) dorsalText.text = text;
        if (dorsal)
        {
            dorsal.transform.localPosition = dorsalOffset;
            dorsal.SetActive(true);
        }
    }

    public void HideDorsal()
    {
        if (dorsal) dorsal.SetActive(false);
    }

    IEnumerator HopTo(Vector3 target)
    {
        Vector3 from = new Vector3(transform.position.x, startPos.y, startPos.z);
        int hops = Mathf.Max(1, Mathf.RoundToInt((target.x - from.x) / stepSize));

        for (int i = 0; i < hops; i++)
        {
            Vector3 a = Vector3.Lerp(from, target, i / (float)hops);
            Vector3 b = Vector3.Lerp(from, target, (i + 1) / (float)hops);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / hopDuration;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * hopHeight;
                transform.position = p;
                yield return null;
            }
        }

        transform.position = target;
        moveRoutine = null;
    }
}
