using UnityEngine;

public class MarqueeLights : MonoBehaviour
{
    public SpriteRenderer[] bulbs;
    public SpriteRenderer[] glows;
    public Color bulbOn = new Color(1f, 0.86f, 0.55f);
    public Color bulbOff = new Color(0.55f, 0.38f, 0.32f);
    [Range(0f, 1f)] public float glowAlpha = 0.35f;
    public float interval = 0.35f;

    float timer;
    int phase;

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;
        phase = 1 - phase;
        Apply();
    }

    void Apply()
    {
        if (bulbs == null) return;
        for (int i = 0; i < bulbs.Length; i++)
        {
            bool on = i % 2 == phase;
            if (bulbs[i]) bulbs[i].color = on ? bulbOn : bulbOff;
            if (glows != null && i < glows.Length && glows[i])
            {
                var c = bulbOn;
                c.a = on ? glowAlpha : 0f;
                glows[i].color = c;
            }
        }
    }
}
