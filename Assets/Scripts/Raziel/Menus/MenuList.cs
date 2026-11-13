using System;
using UnityEngine;

public class MenuList : MonoBehaviour
{
    public MenuButton[] buttons;
    public RectTransform arrow;
    public float arrowGap = 36f;

    public event Action<int> SelectionChanged;
    public event Action<int> Submitted;
    public event Action<int, int> Horizontal;

    public int Selected { get; private set; }

    public bool Locked
    {
        get => locked;
        set
        {
            if (locked && !value) readyFrame = Time.frameCount;
            locked = value;
        }
    }

    bool locked;
    int readyFrame = -1;

    void Awake()
    {
        foreach (var b in buttons)
        {
            b.Hovered += OnHover;
            b.Clicked += OnClick;
        }
    }

    void OnEnable()
    {
        readyFrame = Time.frameCount;
        Select(Selected, false);
    }

    public void Select(int index, bool animate = true)
    {
        if (buttons == null || buttons.Length == 0) return;
        index = (index % buttons.Length + buttons.Length) % buttons.Length;
        bool changed = index != Selected;
        Selected = index;
        for (int i = 0; i < buttons.Length; i++) buttons[i].SetSelected(i == index, animate && i == index);
        if (changed || !animate) SelectionChanged?.Invoke(index);
    }

    void OnHover(MenuButton b)
    {
        if (locked) return;
        int i = Array.IndexOf(buttons, b);
        if (i >= 0 && i != Selected) Select(i);
    }

    void OnClick(MenuButton b)
    {
        if (locked) return;
        int i = Array.IndexOf(buttons, b);
        if (i < 0) return;
        if (i != Selected) Select(i);
        Submitted?.Invoke(i);
    }

    void Update()
    {
        MoveArrow();
        if (locked || Time.frameCount == readyFrame) return;
        if (MenuInput.Up) Select(Selected - 1);
        else if (MenuInput.Down) Select(Selected + 1);
        else if (MenuInput.Left) Horizontal?.Invoke(Selected, -1);
        else if (MenuInput.Right) Horizontal?.Invoke(Selected, 1);
        else if (MenuInput.Submit) Submitted?.Invoke(Selected);
    }

    void MoveArrow()
    {
        if (!arrow || buttons == null || buttons.Length == 0) return;
        var b = (RectTransform)buttons[Selected].transform;
        float bob = Mathf.Sin(Time.unscaledTime * 6f) * 6f;
        var target = b.anchoredPosition + new Vector2(-b.sizeDelta.x * 0.5f - arrowGap + bob, 0f);
        arrow.anchoredPosition = Vector2.Lerp(arrow.anchoredPosition, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
    }
}
