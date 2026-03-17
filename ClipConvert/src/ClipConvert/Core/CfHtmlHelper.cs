namespace ClipConvert.Core;

/// <summary>
/// Handles parsing and generating the CF_HTML clipboard format header
/// required by Windows for HTML clipboard data.
/// </summary>
public static class CfHtmlHelper
{
    private const string Header =
        "Version:0.9\r\n" +
        "StartHTML:{0:0000000000}\r\n" +
        "EndHTML:{1:0000000000}\r\n" +
        "StartFragment:{2:0000000000}\r\n" +
        "EndFragment:{3:0000000000}\r\n";

    private const string HtmlPrefix = "<html><body>\r\n<!--StartFragment-->";
    private const string HtmlSuffix = "<!--EndFragment-->\r\n</body></html>";

    /// <summary>
    /// Wraps an HTML fragment with the CF_HTML header that Windows expects.
    /// </summary>
    public static string Encode(string htmlFragment)
    {
        // Calculate a preliminary header to determine its byte length
        var dummyHeader = string.Format(Header, 0, 0, 0, 0);
        int headerLength = System.Text.Encoding.UTF8.GetByteCount(dummyHeader);

        int startHtml = headerLength;
        int startFragment = headerLength + System.Text.Encoding.UTF8.GetByteCount(HtmlPrefix);
        int endFragment = startFragment + System.Text.Encoding.UTF8.GetByteCount(htmlFragment);
        int endHtml = endFragment + System.Text.Encoding.UTF8.GetByteCount(HtmlSuffix);

        var result = string.Format(Header, startHtml, endHtml, startFragment, endFragment);
        result += HtmlPrefix + htmlFragment + HtmlSuffix;

        return result;
    }

    /// <summary>
    /// Extracts the HTML fragment from a CF_HTML formatted string.
    /// </summary>
    public static string? Decode(string cfHtml)
    {
        int startFragment = GetOffset(cfHtml, "StartFragment:");
        int endFragment = GetOffset(cfHtml, "EndFragment:");

        if (startFragment < 0 || endFragment < 0 || endFragment <= startFragment)
            return null;

        // CF_HTML offsets are byte offsets in the UTF-8 encoded string
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(cfHtml);
        if (endFragment > bytes.Length)
            return null;

        return System.Text.Encoding.UTF8.GetString(bytes, startFragment, endFragment - startFragment);
    }

    private static int GetOffset(string cfHtml, string marker)
    {
        int idx = cfHtml.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return -1;

        int start = idx + marker.Length;
        int end = cfHtml.IndexOf('\r', start);
        if (end < 0) end = cfHtml.IndexOf('\n', start);
        if (end < 0) end = cfHtml.Length;

        var value = cfHtml[start..end].Trim();
        return int.TryParse(value, out int offset) ? offset : -1;
    }
}
