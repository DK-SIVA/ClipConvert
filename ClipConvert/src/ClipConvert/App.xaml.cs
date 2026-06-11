using System.Windows;
using ClipConvert.Core;
using ClipConvert.Models;
using ClipConvert.Services;
using ClipConvert.UI;

namespace ClipConvert;

public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private ClipboardManager? _clipboardManager;
    private TrayIconManager? _trayIconManager;
    private SettingsService? _settingsService;
    private UpdateService? _updateService;
    private AppSettings? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure single instance
        var mutex = new System.Threading.Mutex(true, "ClipConvert_SingleInstance", out bool isNew);
        if (!isNew)
        {
            System.Windows.MessageBox.Show("ClipConvert läuft bereits.", "ClipConvert",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        GC.KeepAlive(mutex);

        // Load settings
        _settingsService = new SettingsService();
        _settings = _settingsService.Load();

        // Initialize core services
        var engine = new ConversionEngine();
        _clipboardManager = new ClipboardManager(engine);

        // Initialize tray icon (before hotkeys so we can show balloon errors)
        _trayIconManager = new TrayIconManager();
        _trayIconManager.OnExitRequested += () => Shutdown();
        _trayIconManager.OnSettingsRequested += HandleOpenSettings;
        _trayIconManager.OnUpdateCheckRequested += () => _ = CheckForUpdatesAsync(manual: true);
        _trayIconManager.Initialize(_settings);

        // Check GitHub for a newer release in the background
        _updateService = new UpdateService();
        if (_settings.CheckForUpdates)
            _ = CheckForUpdatesAsync(manual: false);

        // Register global hotkeys
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.OnConvertToMarkdown += HandleConvertToMarkdown;
        _hotkeyManager.OnConvertToRichText += HandleConvertToRichText;
        _hotkeyManager.Register(_settings.ToMarkdownHotkey, _settings.ToRichTextHotkey);

        if (_hotkeyManager.LastRegistrationError != null)
        {
            _trayIconManager.ShowBalloon("ClipConvert – Hotkey-Fehler",
                _hotkeyManager.LastRegistrationError, System.Windows.Forms.ToolTipIcon.Warning);
        }
        else
        {
            _trayIconManager.ShowBalloon("ClipConvert gestartet",
                $"{_settings.ToMarkdownHotkey} → Markdown\n{_settings.ToRichTextHotkey} → Rich Text");
        }
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        try
        {
            // Don't compete with app startup; the manual check should respond immediately
            if (!manual)
                await Task.Delay(TimeSpan.FromSeconds(10));

            var update = await _updateService!.CheckForUpdateAsync();
            if (update == null)
            {
                if (manual)
                {
                    System.Windows.MessageBox.Show(
                        $"ClipConvert ist aktuell (v{UpdateService.CurrentVersion}).",
                        "ClipConvert – Update", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Eine neue Version ist verfügbar: v{update.Version}\n" +
                $"Installierte Version: v{UpdateService.CurrentVersion}\n\n" +
                "Jetzt herunterladen und installieren?\n" +
                "ClipConvert wird dafür kurz beendet und automatisch neu gestartet.",
                "ClipConvert – Update verfügbar", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            await _updateService.DownloadAndInstallAsync(update);
            Shutdown();
        }
        catch (Exception ex)
        {
            // Silent background check stays silent on errors (e.g. offline)
            if (manual)
            {
                System.Windows.MessageBox.Show(
                    $"Update-Prüfung fehlgeschlagen:\n{ex.Message}",
                    "ClipConvert – Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void HandleOpenSettings()
    {
        var settingsWindow = new SettingsWindow(_settings!.ToMarkdownHotkey, _settings.ToRichTextHotkey);
        settingsWindow.ShowDialog();

        if (settingsWindow.Saved)
        {
            _settings.ToMarkdownHotkey = settingsWindow.ResultMarkdownHotkey;
            _settings.ToRichTextHotkey = settingsWindow.ResultRichTextHotkey;
            _settingsService!.Save(_settings);

            // Re-register hotkeys with new bindings
            _hotkeyManager!.Register(_settings.ToMarkdownHotkey, _settings.ToRichTextHotkey);
            _trayIconManager!.UpdateHotkeyLabels(_settings);

            if (_hotkeyManager.LastRegistrationError != null)
            {
                _trayIconManager.ShowBalloon("ClipConvert – Hotkey-Fehler",
                    _hotkeyManager.LastRegistrationError, System.Windows.Forms.ToolTipIcon.Warning);
            }
            else
            {
                _trayIconManager.ShowBalloon("ClipConvert",
                    $"Hotkeys aktualisiert:\n{_settings.ToMarkdownHotkey} → Markdown\n{_settings.ToRichTextHotkey} → Rich Text");
            }
        }
    }

    private void HandleConvertToMarkdown()
    {
        var result = _clipboardManager!.ConvertToMarkdown();
        if (result.IsSuccess)
        {
            if (_settings!.AutoPaste)
                KeyboardSimulator.SchedulePaste();
        }
        else
        {
            _trayIconManager?.ShowBalloon("ClipConvert – Fehler",
                result.Error ?? "Konvertierung fehlgeschlagen.",
                System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    private void HandleConvertToRichText()
    {
        var result = _clipboardManager!.ConvertToRichText();
        if (result.IsSuccess)
        {
            if (_settings!.AutoPaste)
                KeyboardSimulator.SchedulePaste();
        }
        else
        {
            _trayIconManager?.ShowBalloon("ClipConvert – Fehler",
                result.Error ?? "Konvertierung fehlgeschlagen.",
                System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();
        base.OnExit(e);
    }
}
