using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipConvert.Core;

/// <summary>
/// Simulates Ctrl+V paste using WinForms SendKeys (most reliable high-level API)
/// combined with a Timer to ensure proper timing after hotkey release.
/// </summary>
public static class KeyboardSimulator
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;

    /// <summary>
    /// Schedules a Ctrl+V paste using a WinForms Timer.
    /// The Timer fires on the UI thread's message loop, which is required for SendKeys.
    /// </summary>
    public static void SchedulePaste()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 50 };
        int ticks = 0;

        timer.Tick += (_, _) =>
        {
            ticks++;

            // Wait for modifiers to be released (max ~2 seconds)
            if (ticks < 40 && (IsKeyDown(VK_CONTROL) || IsKeyDown(VK_SHIFT) ||
                               IsKeyDown(VK_MENU) || IsKeyDown(VK_LWIN)))
            {
                return; // Keep waiting
            }

            timer.Stop();
            timer.Dispose();

            // Send Ctrl+V via SendKeys (SendWait works without app message pump)
            SendKeys.SendWait("^v");
        };

        timer.Start();
    }

    private static bool IsKeyDown(int vk)
    {
        return (GetAsyncKeyState(vk) & 0x8000) != 0;
    }
}
