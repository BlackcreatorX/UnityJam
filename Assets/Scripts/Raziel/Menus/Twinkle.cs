using UnityEngine;
using UnityEngine.UI;

public class Twinkle : MonoBehaviour
{
    public float speed = 2f;
    public float offset;
    [Range(0f, 1f)] public float minScale = 0.55f;
    public bool unscaledTime = true;

    Vector3 baseScale;
    Image image;

    void Awake()
    {
        baseScale = transform.localScale;
        image = GetComponent<Image>();
    }

    void Update()
    {
        float t = (unscaledTime ? Time.unscaledTime : Time.time) * speed + offset;
        float k = (Mathf.Sin(t) + 1f) * 0.5f;
        transform.localScale = baseScale * Mathf.Lerp(minScale, 1f, k);
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * 12f);
        if (image)
        {
            var c = image.color;
            c.a = Mathf.Lerp(0.35f, 1f, k);
            image.color = c;
        }
    }
}
