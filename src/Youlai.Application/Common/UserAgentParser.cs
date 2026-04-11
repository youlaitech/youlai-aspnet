namespace Youlai.Application.Common;

/// <summary>
/// User Agent 解析工具
/// </summary>
public static class UserAgentParser
{
    /// <summary>
    /// 解析 User Agent 字符串，提取浏览器和操作系统信息
    /// </summary>
    public static (string Browser, string Os) ParseUserAgent(string ua)
    {
        if (string.IsNullOrEmpty(ua))
            return (string.Empty, string.Empty);

        // 操作系统检测
        string os = ua switch
        {
            _ when ua.Contains("Windows NT 10") => "Windows 10",
            _ when ua.Contains("Windows NT 6.3") => "Windows 8.1",
            _ when ua.Contains("Windows NT 6.1") => "Windows 7",
            _ when ua.Contains("Windows") => "Windows",
            _ when ua.Contains("Mac OS X") => ParseOsVersion(ua, "Mac OS X ", "macOS "),
            _ when ua.Contains("Android") => ParseOsVersion(ua, "Android ", "Android "),
            _ when ua.Contains("iPhone") || ua.Contains("iPad") => ParseOsVersion(ua, "OS ", "iOS "),
            _ when ua.Contains("Linux") => "Linux",
            _ => string.Empty
        };

        // 浏览器检测（顺序重要 - Edge 优先于 Chrome）
        string browser;
        if (ua.Contains("Edg/"))
        {
            browser = ParseBrowserVersion(ua, "Edg/", "Edge");
        }
        else if (ua.Contains("OPR/") || ua.Contains("Opera/"))
        {
            browser = ua.Contains("OPR/") ? ParseBrowserVersion(ua, "OPR/", "Opera") : ParseBrowserVersion(ua, "Opera/", "Opera");
        }
        else if (ua.Contains("Firefox/"))
        {
            browser = ParseBrowserVersion(ua, "Firefox/", "Firefox");
        }
        else if (ua.Contains("Chrome/") && !ua.Contains("Edg/"))
        {
            browser = ParseBrowserVersion(ua, "Chrome/", "Chrome");
        }
        else if (ua.Contains("Safari/") && !ua.Contains("Chrome"))
        {
            browser = ParseBrowserVersion(ua, "Version/", "Safari");
        }
        else if (ua.Contains("MSIE") || ua.Contains("Trident/"))
        {
            browser = "IE";
        }
        else
        {
            browser = string.Empty;
        }

        return (browser, os);
    }

    private static string ParseOsVersion(string ua, string prefix, string osName)
    {
        var idx = ua.IndexOf(prefix);
        if (idx == -1) return osName;
        var start = idx + prefix.Length;
        var end = Math.Min(start + 10, ua.Length);
        for (var i = start; i < end; i++)
        {
            if (!char.IsDigit(ua[i]) && ua[i] != '.' && ua[i] != '_')
            {
                var version = ua[start..i].Replace("_", ".");
                return $"{osName}{version}";
            }
        }
        return osName;
    }

    private static string ParseBrowserVersion(string ua, string token, string browserName)
    {
        var idx = ua.IndexOf(token);
        if (idx == -1) return browserName;
        var start = idx + token.Length;
        var end = Math.Min(start + 20, ua.Length);
        for (var i = start; i < end; i++)
        {
            if (!char.IsDigit(ua[i]) && ua[i] != '.')
            {
                return $"{browserName} {ua[start..i]}";
            }
        }
        return browserName;
    }
}
