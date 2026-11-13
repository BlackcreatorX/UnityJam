using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public GameObject root;
    public Image overlay;
    [Range(0f, 1f)] public float overlayAlpha = 0.6f;
    public RectTransform curtains;
    public RectTransform card;
    public RectTransform villain;
    public float villainOffscreen = 700f;
    public MenuList list;
    public OptionsMenu options;
    public Image fade;
    public string mainMenuScene = "MainMenu";
    public float animTime = 0.3f;
    public UnityEvent<bool> onPauseChanged = new UnityEvent<bool>();

    public bool IsPaused { get; private set; }

    Vector2 villainHome;
    float progress;
    Coroutine anim;
    int panelClosedFrame = -1;
    bool leaving;

    void Awake()
    {
        if (FindFirstObjectByType<EventSystem>()) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    void Start()
    {
        OptionsMenu.ApplySaved();
        if (villain) villainHome = villain.anchoredPosition;
        list.Submitted += OnSubmit;
        options.Closed += OnOptionsClosed;
        options.gameObject.SetActive(false);
        Apply(0f);
        root.SetActive(false);
        if (fade)
        {
            fade.color = new Color(0f, 0f, 0f, 0f);
            fade.raycastTarget = false;
        }
    }

    void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f;
    }

    void Update()
    {
        if (leaving || !MenuInput.Back || Time.frameCount == panelClosedFrame) return;
        if (options.gameObject.activeSelf) return;
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || leaving) return;
        IsPaused = true;
        Time.timeScale = 0f;
        root.SetActive(true);
        list.Locked = false;
        list.Select(0, false);
        Play(true);
        onPauseChanged.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        list.Locked = true;
        Time.timeScale = 1f;
        Play(false);
        onPauseChanged.Invoke(false);
    }

    void OnSubmit(int index)
    {
        if (Time.frameCount == panelClosedFrame) return;
        if (index == 0) Resume();
        else if (index == 1)
        {
            list.Locked = true;
            options.Open();
        }
        else StartCoroutine(GoToMenu());
    }

    void OnOptionsClosed()
    {
        panelClosedFrame = Time.frameCount;
        list.Locked = false;
    }

    void Play(bool show)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(show));
    }

    IEnumerator Animate(bool show)
    {
        float target = show ? 1f : 0f;
        while (!Mathf.Approximately(progress, target))
        {
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / animTime);
            Apply(progress);
            yield return null;
        }
        if (!show) root.SetActive(false);
        anim = null;
    }

    void Apply(float p)
    {
        float e = 1f - Mathf.Pow(1f - p, 3f);
        if (overlay)
        {
            var c = overlay.color;
            c.a = overlayAlpha * e;
            overlay.color = c;
        }
        if (curtains) curtains.anchoredPosition = new Vector2(0f, (1f - e) * 1100f);
        if (card) card.localScale = new Vector3(Mathf.Max(0.01f, e), Mathf.Lerp(0.9f, 1f, e), 1f);
        if (villain) villain.anchoredPosition = villainHome + new Vector2((1f - e) * villainOffscreen, 0f);
    }

    IEnumerator GoToMenu()
    {
        leaving = true;
        list.Locked = true;
        if (fade)
        {
            fade.raycastTarget = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.4f;
                fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
                yield return null;
            }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
