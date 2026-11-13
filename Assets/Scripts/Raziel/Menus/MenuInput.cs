using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class MenuInput
{
#if ENABLE_INPUT_SYSTEM
    static bool Pressed(params Key[] keys)
    {
        var k = Keyboard.current;
        if (k == null) return false;
        foreach (var key in keys)
            if (k[key].wasPressedThisFrame) return true;
        return false;
    }

    static bool Pad(System.Func<Gamepad, bool> check)
    {
        var g = Gamepad.current;
        return g != null && check(g);
    }

    public static bool Up => Pressed(Key.UpArrow, Key.W) || Pad(g => g.dpad.up.wasPressedThisFrame);
    public static bool Down => Pressed(Key.DownArrow, Key.S) || Pad(g => g.dpad.down.wasPressedThisFrame);
    public static bool Left => Pressed(Key.LeftArrow, Key.A) || Pad(g => g.dpad.left.wasPressedThisFrame);
    public static bool Right => Pressed(Key.RightArrow, Key.D) || Pad(g => g.dpad.right.wasPressedThisFrame);
    public static bool Submit => Pressed(Key.Enter, Key.NumpadEnter, Key.Space) || Pad(g => g.buttonSouth.wasPressedThisFrame);
    public static bool Back => Pressed(Key.Escape) || Pad(g => g.startButton.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame);
#else
    public static bool Up => Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    public static bool Down => Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
    public static bool Left => Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
    public static bool Right => Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
    public static bool Submit => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
    public static bool Back => Input.GetKeyDown(KeyCode.Escape);
#endif
}
