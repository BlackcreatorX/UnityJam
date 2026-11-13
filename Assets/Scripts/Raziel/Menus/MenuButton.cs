using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public Image background;
    public Text label;
    public Sprite normalSprite;
    public Sprite selectedSprite;
    public Color normalText = new Color32(59, 26, 51, 255);
    public Color selectedText = new Color32(220, 190, 94, 255);

    public event Action<MenuButton> Hovered;
    public event Action<MenuButton> Clicked;

    float punch;

    public void SetSelected(bool on, bool animate)
    {
        if (background) background.sprite = on ? selectedSprite : normalSprite;
        if (label) label.color = on ? selectedText : normalText;
        punch = on && animate ? 1f : 0f;
        if (punch == 0f) transform.localScale = Vector3.one;
    }

    void Update()
    {
        if (punch <= 0f) return;
        punch = Mathf.Max(0f, punch - Time.unscaledDeltaTime * 5f);
        float s = 1f + Mathf.Sin(punch * Mathf.PI) * 0.07f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
    }
}
