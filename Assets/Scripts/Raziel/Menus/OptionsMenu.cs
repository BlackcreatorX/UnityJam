using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public MenuList list;
    public Slider volume;
    public Text volumeLabel;
    public Text fullscreenLabel;
    public RectTransform card;

    public event Action Closed;

    const string VolumeKey = "menu_volume";

    bool fullscreen;
    int openedFrame = -1;
    float openAnim;

    public static void ApplySaved()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
    }

    void Awake()
    {
        list.Submitted += OnSubmit;
        list.Horizontal += OnHorizontal;
        volume.onValueChanged.AddListener(SetVolume);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        openedFrame = Time.frameCount;
        openAnim = 0f;
        fullscreen = Screen.fullScreen;
        volume.SetValueWithoutNotify(AudioListener.volume);
        list.Select(0, false);
        Refresh();
    }

    public void Close()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    void Update()
    {
        var es = EventSystem.current;
        if (es && es.currentSelectedGameObject) es.SetSelectedGameObject(null);

        openAnim = Mathf.MoveTowards(openAnim, 1f, Time.unscaledDeltaTime / 0.2f);
        float e = 1f - Mathf.Pow(1f - openAnim, 3f);
        if (card) card.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);

        if (MenuInput.Back && Time.frameCount != openedFrame) Close();
    }

    void OnSubmit(int index)
    {
        if (index == 1) ToggleFullscreen();
        else if (index == 2) Close();
    }

    void OnHorizontal(int index, int dir)
    {
        if (index == 0) volume.value = Mathf.Clamp01(Mathf.Round((volume.value + dir * 0.1f) * 10f) / 10f);
        else if (index == 1) ToggleFullscreen();
    }

    void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
        Refresh();
    }

    void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        Screen.fullScreen = fullscreen;
        Refresh();
    }

    void Refresh()
    {
        if (volumeLabel) volumeLabel.text = $"VOLUME  {Mathf.RoundToInt(volume.value * 100f)}%";
        if (fullscreenLabel) fullscreenLabel.text = fullscreen ? "FULLSCREEN: ON" : "FULLSCREEN: OFF";
    }
}
