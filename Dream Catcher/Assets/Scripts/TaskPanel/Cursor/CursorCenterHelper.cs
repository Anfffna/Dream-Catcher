using UnityEngine;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
#endif

public static class CursorCenterHelper
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

#endif

    public static void CenterCursor(MonoBehaviour owner)
    {
        if (owner == null)
            return;

        owner.StartCoroutine(CenterCursorCoroutine());
    }

    private static IEnumerator CenterCursorCoroutine()
    {
        WarpToCenter();

        yield return null;

        WarpToCenter();

        yield return null;

        WarpToCenter();
    }

    private static void WarpToCenter()
    {
        Vector2 center = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

#if ENABLE_INPUT_SYSTEM

        if (Mouse.current != null)
            Mouse.current.WarpCursorPosition(center);

#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

        IntPtr window = GetActiveWindow();

        POINT point = new POINT
        {
            X = Mathf.RoundToInt(center.x),
            Y = Mathf.RoundToInt(center.y)
        };

        if (window != IntPtr.Zero)
        {
            ClientToScreen(window, ref point);
            SetCursorPos(point.X, point.Y);
        }
        else
        {
            SetCursorPos(point.X, point.Y);
        }

#endif
    }
}