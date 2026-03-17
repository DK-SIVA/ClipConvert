using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClipConvert.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ClipConvert.UI;

public partial class SettingsWindow : Window
{
    private HotkeyBinding _markdownHotkey;
    private HotkeyBinding _richTextHotkey;

    public HotkeyBinding ResultMarkdownHotkey => _markdownHotkey;
    public HotkeyBinding ResultRichTextHotkey => _richTextHotkey;
    public bool Saved { get; private set; }

    public SettingsWindow(HotkeyBinding currentMarkdown, HotkeyBinding currentRichText)
    {
        InitializeComponent();

        _markdownHotkey = Clone(currentMarkdown);
        _richTextHotkey = Clone(currentRichText);

        TxtMarkdownHotkey.Text = _markdownHotkey.ToString();
        TxtRichTextHotkey.Text = _richTextHotkey.ToString();
    }

    private void TxtMarkdownHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var binding = CaptureHotkey(e);
        if (binding != null)
        {
            _markdownHotkey = binding;
            TxtMarkdownHotkey.Text = binding.ToString();
        }
    }

    private void TxtRichTextHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var binding = CaptureHotkey(e);
        if (binding != null)
        {
            _richTextHotkey = binding;
            TxtRichTextHotkey.Text = binding.ToString();
        }
    }

    private static HotkeyBinding? CaptureHotkey(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore standalone modifier presses
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return null;

        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
        bool win = (Keyboard.Modifiers & ModifierKeys.Windows) != 0;

        // Require at least one modifier
        if (!ctrl && !shift && !alt && !win)
            return null;

        string keyName = KeyToString(key);
        if (string.IsNullOrEmpty(keyName))
            return null;

        return new HotkeyBinding
        {
            Ctrl = ctrl,
            Shift = shift,
            Alt = alt,
            Win = win,
            Key = keyName
        };
    }

    private static string KeyToString(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => key.ToString()[1..], // "D0" → "0"
        >= Key.NumPad0 and <= Key.NumPad9 => "Num" + key.ToString().Last(),
        >= Key.F1 and <= Key.F12 => key.ToString(),
        Key.Space => "Space",
        Key.Tab => "Tab",
        Key.Return => "Return",
        Key.Escape => "Escape",
        Key.Insert => "Insert",
        Key.Delete => "Delete",
        Key.Home => "Home",
        Key.End => "End",
        Key.PageUp => "PageUp",
        Key.PageDown => "PageDown",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemMinus => "-",
        Key.OemPlus => "+",
        _ => ""
    };

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
            tb.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 220));
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
            tb.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!_markdownHotkey.IsValid() || !_richTextHotkey.IsValid())
        {
            System.Windows.MessageBox.Show(
                "Beide Tastenkombinationen müssen gültig sein (Modifier + Taste).",
                "ClipConvert", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Saved = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Saved = false;
        Close();
    }

    private static HotkeyBinding Clone(HotkeyBinding src) => new()
    {
        Ctrl = src.Ctrl,
        Shift = src.Shift,
        Alt = src.Alt,
        Win = src.Win,
        Key = src.Key
    };
}
