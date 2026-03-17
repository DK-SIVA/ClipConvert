using System.Drawing;
using System.Windows.Forms;
using ClipConvert.Models;

namespace ClipConvert.UI;

/// <summary>
/// Manages the system tray icon and its context menu.
/// </summary>
public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ToolStripItem? _markdownLabel;
    private ToolStripItem? _richTextLabel;
    private bool _disposed;

    public event Action? OnExitRequested;
    public event Action? OnSettingsRequested;

    public void Initialize(AppSettings settings)
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("ClipConvert – Aktiv", null).Enabled = false;
        contextMenu.Items.Add(new ToolStripSeparator());
        _markdownLabel = contextMenu.Items.Add($"{settings.ToMarkdownHotkey} → Markdown", null);
        _markdownLabel.Enabled = false;
        _richTextLabel = contextMenu.Items.Add($"{settings.ToRichTextHotkey} → Rich Text", null);
        _richTextLabel.Enabled = false;
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Einstellungen...", null, (_, _) => OnSettingsRequested?.Invoke());
        contextMenu.Items.Add("Info...", null, (_, _) => ShowAbout());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Beenden", null, (_, _) => OnExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "ClipConvert – Markdown ↔ Rich Text",
            Visible = true,
            ContextMenuStrip = contextMenu
        };
    }

    public void UpdateHotkeyLabels(AppSettings settings)
    {
        if (_markdownLabel != null)
            _markdownLabel.Text = $"{settings.ToMarkdownHotkey} → Markdown";
        if (_richTextLabel != null)
            _richTextLabel.Text = $"{settings.ToRichTextHotkey} → Rich Text";
    }

    public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(2000, title, text, icon);
    }

    private static void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            "ClipConvert v1.0\n" +
            "Markdown \u2194 Rich Text Clipboard Converter\n\n" +
            "\u00A9 2026 DeKode. Alle Rechte vorbehalten.",
            "ClipConvert \u2013 Info",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private static Icon CreateDefaultIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.FromArgb(68, 138, 255));
            using var font = new Font("Segoe UI", 12, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            var size = g.MeasureString("M", font);
            g.DrawString("M", font, brush,
                (32 - size.Width) / 2,
                (32 - size.Height) / 2);
        }

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
