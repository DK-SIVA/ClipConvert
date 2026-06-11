# ClipConvert – Markdown ↔ Rich Text Clipboard Converter

## Projektübersicht

Eine Windows Tray-Anwendung, die über globale Hotkeys den Inhalt der Zwischenablage zwischen **Markdown** und **Microsoft Rich Text** (Word, Outlook, OneNote) konvertiert und einfügt.

### Kernidee

Der Nutzer kopiert Text in einem beliebigen Programm (`Ctrl+C`), drückt dann einen speziellen Hotkey, und der Inhalt wird konvertiert in die Zwischenablage geschrieben bzw. direkt eingefügt.

---

## Technologie-Stack

| Komponente | Technologie | Begründung |
|---|---|---|
| **Framework** | .NET 8+ / C# / WPF | Nativer Windows Clipboard-Zugriff (CF_HTML, CF_RTF, CF_UNICODETEXT), System Tray APIs, geringer Overhead |
| **Markdown → HTML** | [Markdig](https://github.com/xoofx/markdig) (C# Library) | Schnell, erweiterbar, CommonMark-kompatibel, kein externer Prozess nötig |
| **HTML → Markdown** | [ReverseMarkdown](https://github.com/mysticmind/reversemarkdown-net) (C# Library) | Solide HTML→Markdown Konvertierung in .NET |
| **Hotkey-Handling** | Win32 `RegisterHotKey` via P/Invoke | Systemweite Hotkeys auch wenn App nicht fokussiert |
| **Tray Icon** | `System.Windows.Forms.NotifyIcon` oder WPF Hardcodetray | Bewährt, stabil |
| **Installer** | WiX Toolset oder Inno Setup | Professioneller Windows-Installer mit Autostart-Option |

### Alternative: Falls du lieber TypeScript/Electron nutzen willst

- **Framework:** Electron mit `clipboard`-Modul
- **Markdown → HTML:** `marked` oder `markdown-it`
- **HTML → Markdown:** `turndown`
- **Hotkeys:** `electron-globalShortcut`
- **Nachteil:** Höherer RAM-Verbrauch (~80-150 MB vs. ~15-30 MB bei .NET)

---

## Architektur

```
┌─────────────────────────────────────────────┐
│              ClipConvert Tray App            │
├─────────────────────────────────────────────┤
│                                             │
│  ┌─────────────┐    ┌────────────────────┐  │
│  │ Hotkey       │───▶│ ClipboardManager   │  │
│  │ Listener     │    │                    │  │
│  └─────────────┘    │ - Read()           │  │
│                     │ - Write()          │  │
│                     │ - DetectFormat()    │  │
│                     └────────┬───────────┘  │
│                              │              │
│                     ┌────────▼───────────┐  │
│                     │ ConversionEngine   │  │
│                     │                    │  │
│                     │ - ToMarkdown()     │  │
│                     │   (HTML→MD via     │  │
│                     │    ReverseMarkdown) │  │
│                     │                    │  │
│                     │ - ToRichText()     │  │
│                     │   (MD→HTML via     │  │
│                     │    Markdig)        │  │
│                     └────────────────────┘  │
│                                             │
│  ┌─────────────┐    ┌────────────────────┐  │
│  │ System Tray  │    │ Settings/Config   │  │
│  │ Icon + Menu  │    │ (JSON file)       │  │
│  └─────────────┘    └────────────────────┘  │
│                                             │
└─────────────────────────────────────────────┘
```

---

## Funktionale Anforderungen

### 1. Zwei globale Hotkeys

| Hotkey | Aktion | Beschreibung |
|---|---|---|
| `Ctrl+Shift+M` | **Einfügen als Markdown** | Liest HTML/RTF aus Clipboard → konvertiert zu Markdown → schreibt Plain Text in Clipboard → simuliert `Ctrl+V` |
| `Ctrl+Shift+R` | **Einfügen als Rich Text** | Liest Plain Text (Markdown) aus Clipboard → konvertiert zu HTML → schreibt HTML in Clipboard (CF_HTML Format) → simuliert `Ctrl+V` |

### 2. Clipboard-Format-Erkennung

Beim Lesen der Zwischenablage soll die App folgende Prioritäten beachten:

**Für "Einfügen als Markdown" (`Ctrl+Shift+M`):**
1. `CF_HTML` prüfen → wenn vorhanden, HTML extrahieren und nach Markdown konvertieren
2. Fallback auf `CF_RTF` → RTF parsen und nach Markdown konvertieren
3. Fallback auf `CF_UNICODETEXT` → Text unverändert einfügen (ist vermutlich schon Markdown)

**Für "Einfügen als Rich Text" (`Ctrl+Shift+R`):**
1. `CF_UNICODETEXT` lesen → als Markdown interpretieren → nach HTML konvertieren
2. HTML im `CF_HTML`-Format in die Zwischenablage schreiben (damit Word/Outlook es als formatierten Text erkennen)

### 3. System Tray

- Tray Icon mit Kontextmenü
- Menüpunkte:
  - **Status-Anzeige:** "Aktiv" / "Pausiert"
  - **Hotkeys anzeigen** (Tooltip oder Untermenü)
  - **Einstellungen** (öffnet Settings-Fenster)
  - **Autostart ein/aus**
  - **Beenden**
- Benachrichtigung (Toast) nach erfolgreicher Konvertierung (optional, abschaltbar)

### 4. Einstellungen (Settings-Fenster)

- Hotkeys konfigurierbar
- Toast-Benachrichtigungen ein/aus
- Autostart mit Windows ein/aus
- Markdown-Variante: CommonMark (Standard), GitHub Flavored Markdown
- Option: Nach Konvertierung automatisch einfügen (Ctrl+V simulieren) oder nur in Clipboard schreiben

---

## Technische Details: Clipboard-Handling unter Windows

### CF_HTML Format

Windows verwendet ein spezielles Format für HTML in der Zwischenablage. Es hat einen Header mit Byte-Offsets:

```
Version:0.9
StartHTML:000000XXX
EndHTML:000000XXX
StartFragment:000000XXX
EndFragment:000000XXX
<html><body>
<!--StartFragment-->
<p>Der eigentliche Inhalt</p>
<!--EndFragment-->
</body></html>
```

**Wichtig:** Beim Schreiben von HTML in die Zwischenablage MUSS dieser Header korrekt generiert werden, sonst erkennen Word/Outlook den Inhalt nicht als formatierten Text.

### Relevante Win32 APIs / .NET Clipboard Methoden

```csharp
// Lesen
Clipboard.ContainsData(DataFormats.Html)    // CF_HTML prüfen
Clipboard.GetData(DataFormats.Html)         // HTML lesen (inkl. Header)
Clipboard.GetText(TextDataFormat.Rtf)       // RTF lesen
Clipboard.GetText(TextDataFormat.UnicodeText) // Plain Text lesen

// Schreiben (mehrere Formate gleichzeitig!)
var dataObject = new DataObject();
dataObject.SetData(DataFormats.Html, cfHtmlString);      // Für Word/Outlook
dataObject.SetText(plainText, TextDataFormat.UnicodeText); // Fallback
Clipboard.SetDataObject(dataObject, true);                 // true = nach App-Ende beibehalten
```

### Globale Hotkeys via P/Invoke

```csharp
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

[DllImport("user32.dll")]
private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

// MOD_CONTROL | MOD_SHIFT = 0x0002 | 0x0004
// VK_M = 0x4D, VK_R = 0x52
```

---

## Projektstruktur

```
ClipConvert/
├── ClipConvert.sln
├── src/
│   └── ClipConvert/
│       ├── ClipConvert.csproj
│       ├── App.xaml                    # WPF Application Entry
│       ├── App.xaml.cs                 # Startup, Tray Init, Single Instance
│       │
│       ├── Core/
│       │   ├── ClipboardManager.cs     # Clipboard lesen/schreiben (CF_HTML, RTF, Text)
│       │   ├── CfHtmlHelper.cs         # CF_HTML Header generieren/parsen
│       │   ├── ConversionEngine.cs     # Markdown↔HTML Konvertierung (Markdig + ReverseMarkdown)
│       │   └── HotkeyManager.cs        # Globale Hotkeys registrieren/verwalten
│       │
│       ├── UI/
│       │   ├── TrayIconManager.cs      # System Tray Icon + Kontextmenü
│       │   ├── SettingsWindow.xaml      # Einstellungsfenster
│       │   └── SettingsWindow.xaml.cs
│       │
│       ├── Models/
│       │   └── AppSettings.cs          # Settings-Modell (JSON serialisierbar)
│       │
│       ├── Services/
│       │   ├── SettingsService.cs       # Settings laden/speichern (%AppData%)
│       │   ├── AutoStartService.cs      # Registry-Eintrag für Autostart
│       │   └── ToastService.cs          # Windows Toast Notifications
│       │
│       └── Assets/
│           └── icon.ico                # Tray Icon
│
├── tests/
│   └── ClipConvert.Tests/
│       ├── ClipConvert.Tests.csproj
│       ├── ConversionEngineTests.cs    # Unit Tests für Markdown↔HTML
│       ├── CfHtmlHelperTests.cs        # CF_HTML Header Tests
│       └── ClipboardManagerTests.cs
│
└── README.md
```

---

## NuGet Packages

```xml
<ItemGroup>
  <!-- Markdown → HTML -->
  <PackageReference Include="Markdig" Version="0.37.*" />
  
  <!-- HTML → Markdown -->
  <PackageReference Include="ReverseMarkdown" Version="4.*" />
  
  <!-- Optional: Für erweiterte HTML Bereinigung vor Konvertierung -->
  <PackageReference Include="HtmlAgilityPack" Version="1.11.*" />
  
  <!-- JSON Settings -->
  <PackageReference Include="System.Text.Json" Version="8.*" />
  
  <!-- Toast Notifications (optional) -->
  <PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.*" />
</ItemGroup>
```

---

## Konvertierungslogik – Details

### Markdown → Rich Text (für Word/Outlook)

```
Input: Markdown (Plain Text aus Clipboard)
  │
  ▼
Markdig Pipeline:
  - CommonMark Parsing
  - GFM Extensions (Tabellen, Task Lists, Strikethrough)
  - Pipe Tables
  │
  ▼
HTML Output (sauberes, semantisches HTML)
  │
  ▼
CSS Inline-Styles hinzufügen:
  - <h1> → font-size: 20pt; font-weight: bold;
  - <code> → font-family: Consolas; background: #f4f4f4;
  - <pre><code> → Codeblock-Styling
  - <table> → border-collapse mit Rahmen
  - <blockquote> → border-left mit Einrückung
  │
  ▼
CF_HTML Header generieren
  │
  ▼
In Clipboard schreiben (CF_HTML + CF_UNICODETEXT Fallback)
```

**Wichtig:** Word/Outlook ignorieren `<style>`-Tags und externe CSS. Alle Styles müssen **inline** sein (`style="..."`).

### Rich Text → Markdown (aus Word/Outlook)

```
Input: CF_HTML aus Clipboard
  │
  ▼
CF_HTML Header parsen → HTML Fragment extrahieren
  │
  ▼
HTML Bereinigung (HtmlAgilityPack):
  - Microsoft-spezifische Tags entfernen (mso-*, MsoNormal, etc.)
  - Leere Spans/Divs entfernen
  - Inline-Styles normalisieren
  │
  ▼
ReverseMarkdown Konvertierung:
  - Config: UnknownTags = PassThrough
  - Config: GithubFlavored = true
  - Config: SmartHrefHandling = true
  │
  ▼
Post-Processing:
  - Überflüssige Leerzeilen entfernen
  - Trailing Whitespace bereinigen
  │
  ▼
In Clipboard als Plain Text schreiben
```

---

## Edge Cases & bekannte Herausforderungen

### Microsoft-spezifisches HTML

Word und Outlook erzeugen extrem aufgeblähtes HTML mit proprietären CSS-Klassen (`MsoNormal`, `mso-style-*`). Dieses muss vor der Markdown-Konvertierung bereinigt werden. Beispiel:

```html
<!-- Was Word in die Zwischenablage legt: -->
<p class="MsoNormal" style="margin-bottom:0cm;line-height:normal">
  <span style="font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;
  mso-fareast-font-family:Calibri;mso-bidi-font-family:&quot;Times New Roman&quot;">
    Einfacher Text
  </span>
</p>

<!-- Was wir daraus machen wollen: -->
Einfacher Text
```

### Bilder

- Bilder in der Zwischenablage sind als Base64 Data-URIs oder als `file:///`-Referenzen eingebettet
- **Erster Ansatz:** Bilder als `![](image)` Platzhalter in Markdown, mit Hinweis
- **Erweiterung (spätere Version):** Bilder in temporären Ordner extrahieren und als lokale Pfade referenzieren

### Tabellen

- Word-Tabellen → HTML `<table>` → Markdown Pipe-Tables (GFM)
- Komplexe Tabellen mit Merged Cells sind in Markdown nicht darstellbar → Fallback auf HTML in Markdown

### Listen-Verschachtelung

- Word verwendet oft eigene Listenformatierung → saubere `<ul>/<ol>` Erkennung nötig
- Nummerierte Listen mit Neustarts beachten

---

## Implementierungsreihenfolge (Phasen)

### Phase 1 – MVP (Kernfunktionalität)
- [ ] .NET 8 WPF Projekt aufsetzen
- [ ] `HotkeyManager`: Globale Hotkeys registrieren (`Ctrl+Shift+M`, `Ctrl+Shift+R`)
- [ ] `ClipboardManager`: Clipboard lesen (CF_HTML, Plain Text) und schreiben
- [ ] `CfHtmlHelper`: CF_HTML Header parsen und generieren
- [ ] `ConversionEngine`: Markdown→HTML (Markdig) und HTML→Markdown (ReverseMarkdown)
- [ ] Basic Tray Icon mit "Beenden"-Option
- [ ] **Ziel:** Kopieren aus Word → Hotkey → Einfügen als Markdown in VS Code funktioniert

### Phase 2 – Polishing
- [ ] Settings-Fenster (Hotkey-Konfiguration, Autostart)
- [ ] `SettingsService`: JSON-basierte Konfiguration in `%AppData%\ClipConvert\`
- [ ] `AutoStartService`: Windows Autostart via Registry
- [ ] Toast-Benachrichtigungen nach Konvertierung
- [ ] HTML-Bereinigung für Microsoft-spezifisches HTML (HtmlAgilityPack)
- [ ] Inline-CSS für Markdown→Rich Text (damit es in Word gut aussieht)

### Phase 3 – Erweiterungen
- [ ] RTF-Support als Fallback (wenn kein CF_HTML vorhanden)
- [ ] Bild-Handling (Base64 → lokale Dateien)
- [ ] Komplexe Tabellen-Konvertierung
- [ ] OneNote-spezifische Bereinigung
- [x] Updater / Auto-Update Mechanismus (GitHub Releases)

### Phase 4 – Distribution
- [ ] Installer (WiX oder Inno Setup)
- [ ] Code Signing
- [ ] GitHub Release Pipeline

---

## Testszenarien

Folgende Szenarien sollten bei der Entwicklung manuell und per Unit Test geprüft werden:

| # | Quelle | Ziel | Inhalt | Erwartetes Ergebnis |
|---|---|---|---|---|
| 1 | Word | VS Code | Einfacher Fließtext mit **Bold** und *Italic* | Sauberes Markdown mit `**` und `*` |
| 2 | Word | VS Code | Überschriften H1-H3 | `#`, `##`, `###` |
| 3 | Word | VS Code | Aufzählung (Bullets) | `- Item` Liste |
| 4 | Word | VS Code | Nummerierte Liste | `1. Item` Liste |
| 5 | Word | VS Code | Tabelle | GFM Pipe Table |
| 6 | Word | VS Code | Hyperlinks | `[Text](URL)` |
| 7 | Outlook | Obsidian | E-Mail mit Formatierung | Sauberes Markdown |
| 8 | VS Code | Word | Markdown mit Code-Block | Formatierter Code-Block mit Monospace |
| 9 | VS Code | Outlook | Markdown mit Tabelle | HTML-Tabelle mit Rahmen |
| 10 | Obsidian | Word | Markdown mit Mixed Content | Korrekt formatiertes Dokument |

---

## Offene Entscheidungen

1. **Soll nach der Konvertierung automatisch `Ctrl+V` simuliert werden?** Oder nur in die Zwischenablage schreiben und der User fügt selbst ein? → Empfehlung: Beides als Option, Standard = automatisch einfügen
2. **Soll die Original-Zwischenablage nach dem Einfügen wiederhergestellt werden?** → Kann UX verbessern, ist aber technisch fragil
3. **Braucht es ein visuelles Preview-Fenster?** → Für MVP: Nein. Später optional.
4. **Lizenzmodell:** MIT? Proprietär?
