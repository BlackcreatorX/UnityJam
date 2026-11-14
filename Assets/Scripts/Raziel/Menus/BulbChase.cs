using UnityEngine;
using UnityEngine.UI;

public class BulbChase : MonoBehaviour
{
    public Image[] glass;
    public Image[] glow;
    public Color onColor = new Color32(255, 214, 120, 255);
    public Color offColor = new Color32(120, 95, 80, 255);
    [Range(0f, 1f)] public float glowAlpha = 0.55f;
    public float interval = 0.3f;
    public int pattern = 3;

    float timer;
    int step;

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < interval) return;
        timer = 0f;
        step = (step + 1) % Mathf.Max(1, pattern);
        Apply();
    }

    void Apply()
    {
        if (glass == null) return;
        int p = Mathf.Max(1, pattern);
        for (int i = 0; i < glass.Length; i++)
        {
            bool on = i % p != step;
            if (glass[i]) glass[i].color = on ? onColor : offColor;
            if (glow != null && i < glow.Length && glow[i])
            {
                var c = onColor;
                c.a = on ? glowAlpha : 0f;
                glow[i].color = c;
            }
        }
    }
}
