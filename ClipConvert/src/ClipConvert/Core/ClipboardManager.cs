using System.IO;
using System.Text;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.DataObject;

namespace ClipConvert.Core;

/// <summary>
/// Reads and writes clipboard data in various formats (CF_HTML, RTF, Unicode Text).
/// </summary>
public class ClipboardManager
{
    private readonly ConversionEngine _engine;

    public ClipboardManager(ConversionEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Converts clipboard content to Markdown and writes it back as plain text.
    /// Priority: CF_HTML → CF_UNICODETEXT (unchanged).
    /// </summary>
    public ConversionResult ConvertToMarkdown()
    {
        try
        {
            var dataObject = WpfClipboard.GetDataObject();
            if (dataObject == null)
                return ConversionResult.Fail("Zwischenablage ist leer.");

            string? markdown = null;

            // Priority 1: CF_HTML
            if (dataObject.GetDataPresent(WpfDataFormats.Html))
            {
                string? cfHtml = dataObject.GetData(WpfDataFormats.Html) as string;
                if (!string.IsNullOrEmpty(cfHtml))
                {
                    string? htmlFragment = CfHtmlHelper.Decode(cfHtml);
                    if (!string.IsNullOrEmpty(htmlFragment))
                    {
                        markdown = _engine.HtmlToMarkdown(htmlFragment);
                    }
                }
            }

            // Priority 2: Plain text (assume it's already usable)
            if (markdown == null && dataObject.GetDataPresent(WpfDataFormats.UnicodeText))
            {
                markdown = dataObject.GetData(WpfDataFormats.UnicodeText) as string;
            }

            if (string.IsNullOrEmpty(markdown))
                return ConversionResult.Fail("Kein konvertierbarer Inhalt in der Zwischenablage.");

            // Write Markdown as plain text — use SetDataObject with copy=true to persist
            var newData = new WpfDataObject();
            newData.SetData(WpfDataFormats.UnicodeText, markdown);
            WpfClipboard.SetDataObject(newData, true);

            return ConversionResult.Success(markdown);
        }
        catch (Exception ex)
        {
            return ConversionResult.Fail($"Fehler bei Konvertierung: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts clipboard plain text (Markdown) to Rich Text (CF_HTML) and writes it back.
    /// </summary>
    public ConversionResult ConvertToRichText()
    {
        try
        {
            string? markdownText = null;
            var dataObject = WpfClipboard.GetDataObject();
            if (dataObject != null && dataObject.GetDataPresent(WpfDataFormats.UnicodeText))
            {
                markdownText = dataObject.GetData(WpfDataFormats.UnicodeText) as string;
            }

            if (string.IsNullOrEmpty(markdownText))
                return ConversionResult.Fail("Kein Text in der Zwischenablage.");

            string html = _engine.MarkdownToHtml(markdownText);
            string cfHtml = CfHtmlHelper.Encode(html);

            // CF_HTML must be set as UTF-8 encoded MemoryStream for Word/Outlook to recognize it
            byte[] cfHtmlBytes = Encoding.UTF8.GetBytes(cfHtml);
            var htmlStream = new MemoryStream(cfHtmlBytes);

            var newData = new WpfDataObject();
            newData.SetData(WpfDataFormats.Html, htmlStream);
            newData.SetData(WpfDataFormats.UnicodeText, markdownText);
            WpfClipboard.SetDataObject(newData, true);

            return ConversionResult.Success(html);
        }
        catch (Exception ex)
        {
            return ConversionResult.Fail($"Fehler bei Konvertierung: {ex.Message}");
        }
    }
}

public record ConversionResult(bool IsSuccess, string Content, string? Error = null)
{
    public static ConversionResult Success(string content) => new(true, content);
    public static ConversionResult Fail(string error) => new(false, string.Empty, error);
}
