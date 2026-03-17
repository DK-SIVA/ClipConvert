using Markdig;
using ReverseMarkdown;

namespace ClipConvert.Core;

/// <summary>
/// Converts between Markdown and HTML using Markdig and ReverseMarkdown.
/// </summary>
public class ConversionEngine
{
    private readonly MarkdownPipeline _markdownPipeline;
    private readonly ReverseMarkdown.Converter _htmlToMdConverter;

    public ConversionEngine()
    {
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        _htmlToMdConverter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config
        {
            UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.PassThrough,
            GithubFlavored = true,
            SmartHrefHandling = true,
            RemoveComments = true
        });
    }

    /// <summary>
    /// Converts Markdown text to HTML with inline styles suitable for Word/Outlook.
    /// </summary>
    public string MarkdownToHtml(string markdown)
    {
        string rawHtml = Markdig.Markdown.ToHtml(markdown, _markdownPipeline);
        return AddInlineStyles(rawHtml);
    }

    /// <summary>
    /// Converts HTML to Markdown, cleaning up Microsoft-specific markup.
    /// </summary>
    public string HtmlToMarkdown(string html)
    {
        string cleaned = CleanMicrosoftHtml(html);
        string markdown = _htmlToMdConverter.Convert(cleaned);
        return PostProcess(markdown);
    }

    private static string AddInlineStyles(string html)
    {
        // Inline styles so Word/Outlook render them correctly (they ignore <style> tags)
        html = html.Replace("<h1>", "<h1 style=\"font-size:20pt;font-weight:bold;margin:12pt 0 6pt 0;\">");
        html = html.Replace("<h2>", "<h2 style=\"font-size:16pt;font-weight:bold;margin:10pt 0 4pt 0;\">");
        html = html.Replace("<h3>", "<h3 style=\"font-size:13pt;font-weight:bold;margin:8pt 0 4pt 0;\">");
        html = html.Replace("<h4>", "<h4 style=\"font-size:11pt;font-weight:bold;margin:6pt 0 2pt 0;\">");
        html = html.Replace("<blockquote>",
            "<blockquote style=\"border-left:3px solid #ccc;padding-left:12px;margin:8pt 0;color:#555;\">");
        html = html.Replace("<code>",
            "<code style=\"font-family:Consolas,'Courier New',monospace;background:#f4f4f4;padding:2px 4px;font-size:10pt;\">");
        html = html.Replace("<pre>",
            "<pre style=\"background:#f4f4f4;padding:10px;border:1px solid #ddd;overflow-x:auto;font-size:10pt;\">");
        html = html.Replace("<table>",
            "<table style=\"border-collapse:collapse;margin:8pt 0;\">");
        html = html.Replace("<th>",
            "<th style=\"border:1px solid #999;padding:6px 10px;background:#f0f0f0;font-weight:bold;\">");
        html = html.Replace("<td>",
            "<td style=\"border:1px solid #999;padding:6px 10px;\">");
        html = html.Replace("<p>", "<p style=\"margin:0 0 8pt 0;\">");

        return html;
    }

    private static string CleanMicrosoftHtml(string html)
    {
        // Remove Microsoft-specific CSS classes and styles
        // Basic cleaning for MVP — HtmlAgilityPack will be added in Phase 2
        html = System.Text.RegularExpressions.Regex.Replace(html, @"class=""Mso\w+""", "");
        html = System.Text.RegularExpressions.Regex.Replace(html, @"mso-[^;""]+;?", "");
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<o:p>.*?</o:p>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<!\[if.*?\]>.*?<!\[endif\]>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<!--\[if.*?\]>.*?<!\[endif\]-->", "", System.Text.RegularExpressions.RegexOptions.Singleline);

        return html;
    }

    private static string PostProcess(string markdown)
    {
        // Remove excessive blank lines (3+ → 2)
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\n{3,}", "\n\n");
        // Trim trailing whitespace on each line
        markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"[ \t]+\n", "\n");
        return markdown.Trim();
    }
}
