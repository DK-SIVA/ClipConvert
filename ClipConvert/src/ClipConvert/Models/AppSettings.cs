namespace ClipConvert.Models;

public class AppSettings
{
    public bool AutoPaste { get; set; } = true;
    public bool ShowToastNotifications { get; set; } = false;
    public bool AutoStart { get; set; } = false;
    public bool CheckForUpdates { get; set; } = true;

    public HotkeyBinding ToMarkdownHotkey { get; set; } = new()
    {
        Ctrl = true, Shift = true, Alt = false, Key = "M"
    };

    public HotkeyBinding ToRichTextHotkey { get; set; } = new()
    {
        Ctrl = true, Shift = true, Alt = false, Key = "R"
    };
}

public class HotkeyBinding
{
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }
    public bool Win { get; set; }
    public string Key { get; set; } = "";

    /// <summary>
    /// Returns the Win32 modifier flags for RegisterHotKey.
    /// </summary>
    public uint GetModifiers()
    {
        uint mods = 0;
        if (Ctrl) mods |= 0x0002;   // MOD_CONTROL
        if (Alt) mods |= 0x0001;    // MOD_ALT
        if (Shift) mods |= 0x0004;  // MOD_SHIFT
        if (Win) mods |= 0x0008;    // MOD_WIN
        return mods;
    }

    /// <summary>
    /// Returns the virtual key code for the bound key.
    /// </summary>
    public uint GetVirtualKey()
    {
        if (string.IsNullOrEmpty(Key)) return 0;

        // Single letter A-Z
        if (Key.Length == 1 && char.IsLetter(Key[0]))
            return (uint)char.ToUpper(Key[0]);

        // Single digit 0-9
        if (Key.Length == 1 && char.IsDigit(Key[0]))
            return (uint)Key[0];

        // Function keys F1-F12
        if (Key.StartsWith("F") && int.TryParse(Key[1..], out int fNum) && fNum >= 1 && fNum <= 12)
            return (uint)(0x70 + fNum - 1); // VK_F1 = 0x70

        return Key.ToUpper() switch
        {
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "RETURN" or "ENTER" => 0x0D,
            "ESCAPE" or "ESC" => 0x1B,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            _ => 0
        };
    }

    public bool IsValid() => GetModifiers() != 0 && GetVirtualKey() != 0;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        if (!string.IsNullOrEmpty(Key)) parts.Add(Key.ToUpper());
        return parts.Count > 0 ? string.Join(" + ", parts) : "(Nicht belegt)";
    }
}
