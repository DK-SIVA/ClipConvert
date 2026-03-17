using System.Runtime.InteropServices;
using System.Windows.Forms;
using ClipConvert.Models;

namespace ClipConvert.Core;

/// <summary>
/// Registers and manages system-wide global hotkeys via Win32 RegisterHotKey.
/// Uses a dedicated NativeWindow for reliable WM_HOTKEY message processing.
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID_TO_MARKDOWN = 0x0001;
    private const int HOTKEY_ID_TO_RICHTEXT = 0x0002;
    private const int WM_HOTKEY = 0x0312;

    private readonly HotkeyNativeWindow _nativeWindow;
    private bool _disposed;

    public event Action? OnConvertToMarkdown;
    public event Action? OnConvertToRichText;

    public string? LastRegistrationError { get; private set; }

    public HotkeyManager()
    {
        _nativeWindow = new HotkeyNativeWindow(this);
        _nativeWindow.CreateHandle(new CreateParams());
    }

    /// <summary>
    /// Registers the global hotkeys with the given bindings.
    /// Can be called multiple times to re-register with new bindings.
    /// </summary>
    public void Register(HotkeyBinding markdownHotkey, HotkeyBinding richTextHotkey)
    {
        LastRegistrationError = null;
        var handle = _nativeWindow.Handle;

        // Unregister previous bindings first
        UnregisterHotKey(handle, HOTKEY_ID_TO_MARKDOWN);
        UnregisterHotKey(handle, HOTKEY_ID_TO_RICHTEXT);

        var errors = new List<string>();

        if (markdownHotkey.IsValid())
        {
            bool ok = RegisterHotKey(handle, HOTKEY_ID_TO_MARKDOWN,
                markdownHotkey.GetModifiers(), markdownHotkey.GetVirtualKey());
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                errors.Add($"Konnte {markdownHotkey} nicht registrieren (Win32 Error {err} – evtl. bereits von anderer App belegt).");
            }
        }
        else
        {
            errors.Add($"Markdown-Hotkey ist ungültig: {markdownHotkey}");
        }

        if (richTextHotkey.IsValid())
        {
            bool ok = RegisterHotKey(handle, HOTKEY_ID_TO_RICHTEXT,
                richTextHotkey.GetModifiers(), richTextHotkey.GetVirtualKey());
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                errors.Add($"Konnte {richTextHotkey} nicht registrieren (Win32 Error {err} – evtl. bereits von anderer App belegt).");
            }
        }
        else
        {
            errors.Add($"Rich-Text-Hotkey ist ungültig: {richTextHotkey}");
        }

        if (errors.Count > 0)
            LastRegistrationError = string.Join("\n", errors);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var handle = _nativeWindow.Handle;
        UnregisterHotKey(handle, HOTKEY_ID_TO_MARKDOWN);
        UnregisterHotKey(handle, HOTKEY_ID_TO_RICHTEXT);
        _nativeWindow.DestroyHandle();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dedicated NativeWindow subclass to receive WM_HOTKEY messages.
    /// </summary>
    private class HotkeyNativeWindow : NativeWindow
    {
        private readonly HotkeyManager _owner;

        public HotkeyNativeWindow(HotkeyManager owner) => _owner = owner;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_ID_TO_MARKDOWN:
                        _owner.OnConvertToMarkdown?.Invoke();
                        return;
                    case HOTKEY_ID_TO_RICHTEXT:
                        _owner.OnConvertToRichText?.Invoke();
                        return;
                }
            }

            base.WndProc(ref m);
        }
    }
}
