using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public MenuList list;
    public RectTransform wheel;
    public float degreesPerOption = 54f;
    public float[] optionAngles = { 0f, 54f, 126f, 180f };
    public float wheelSmooth = 0.25f;
    public Image villain;
    public float villainScale = 0.82f;
    public Sprite villainIdle;
    public Sprite villainPlay;
    public Sprite villainQuit;
    public OptionsMenu options;
    public GameObject credits;
    public MenuList creditsList;
    public Image fade;
    public string gameScene = "PauseTest";

    float wheelAngle;
    float wheelTarget;
    float wheelVelocity;
    bool leaving;
    int panelClosedFrame = -1;
    int creditsOpenedFrame = -1;

    void Start()
    {
        Time.timeScale = 1f;
        OptionsMenu.ApplySaved();
        list.SelectionChanged += OnSelectionChanged;
        list.Submitted += OnSubmit;
        options.Closed += OnPanelClosed;
        creditsList.Submitted += _ => CloseCredits();
        options.gameObject.SetActive(false);
        credits.SetActive(false);
        SetVillain(villainIdle);
        StartCoroutine(Fade(1f, 0f, 0.6f));
    }

    void Update()
    {
        if (leaving) return;

        wheelAngle = Mathf.SmoothDampAngle(wheelAngle, wheelTarget, ref wheelVelocity, wheelSmooth, Mathf.Infinity, Time.unscaledDeltaTime);
        wheel.localEulerAngles = new Vector3(0f, 0f, wheelAngle + Mathf.Sin(Time.unscaledTime * 1.3f) * 1.5f);

        if (credits.activeSelf && MenuInput.Back && Time.frameCount != creditsOpenedFrame) CloseCredits();
    }

    void OnSelectionChanged(int index)
    {
        wheelTarget = optionAngles != null && index < optionAngles.Length ? -optionAngles[index] : -index * degreesPerOption;
        SetVillain(index == list.buttons.Length - 1 ? villainQuit : villainIdle);
    }

    void OnSubmit(int index)
    {
        if (leaving || Time.frameCount == panelClosedFrame) return;
        switch (index)
        {
            case 0: StartCoroutine(Play()); break;
            case 1: OpenOptions(); break;
            case 2: OpenCredits(); break;
            default: StartCoroutine(Quit()); break;
        }
    }

    void OpenOptions()
    {
        list.Locked = true;
        options.Open();
    }

    void OpenCredits()
    {
        list.Locked = true;
        credits.SetActive(true);
        creditsOpenedFrame = Time.frameCount;
        creditsList.Select(0, false);
    }

    void CloseCredits()
    {
        credits.SetActive(false);
        OnPanelClosed();
    }

    void OnPanelClosed()
    {
        panelClosedFrame = Time.frameCount;
        list.Locked = false;
    }

    void SetVillain(Sprite s)
    {
        if (!villain || !s) return;
        villain.sprite = s;
        villain.rectTransform.sizeDelta = s.rect.size * villainScale;
    }

    IEnumerator Play()
    {
        leaving = true;
        list.Locked = true;
        SetVillain(villainPlay);
        float start = wheelAngle;
        float end = start - 1080f - 360f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 1.8f;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            wheel.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(start, end, e));
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
        yield return Fade(0f, 1f, 0.5f);
        SceneManager.LoadScene(gameScene);
    }

    IEnumerator Quit()
    {
        leaving = true;
        list.Locked = true;
        yield return Fade(0f, 1f, 0.4f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (!fade) yield break;
        fade.raycastTarget = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            var c = fade.color;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            fade.color = c;
            yield return null;
        }
        fade.raycastTarget = to > 0f;
    }
}
