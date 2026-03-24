using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Youlai.Application.Common.Attributes;
using Youlai.Application.Common.Enums;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Common.Filters;

/// <summary>
/// 操作日志 Action Filter
/// 拦截带有 [Log] 属性的控制器方法，在执行后记录操作日志
/// </summary>
public sealed class LogActionFilter : IAsyncActionFilter
{
    private readonly YoulaiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogActionFilter(YoulaiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata;
        var logAttr = endpoint.OfType<LogAttribute>().FirstOrDefault();

        if (logAttr == null)
        {
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var actionExecutedContext = await next();
        stopwatch.Stop();

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var (browser, os) = ParseUserAgent(userAgent);

            long? operatorId = null;
            string? operatorName = null;
            var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst("userId");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var uid))
            {
                operatorId = uid;
            }
            var usernameClaim = httpContext.User.FindFirst("username") ?? httpContext.User.FindFirst("nickname");
            if (usernameClaim != null)
            {
                operatorName = usernameClaim.Value;
            }

            // 构建标题
            var title = logAttr.Title;
            if (string.IsNullOrEmpty(title))
            {
                title = GetEnumDescription(logAttr.Module) + "-" + GetEnumDescription(logAttr.Value);
            }

            var log = new SysLog
            {
                Module = (int)logAttr.Module,
                ActionType = (int)logAttr.Value,
                Title = title,
                Content = logAttr.Content,
                OperatorId = operatorId,
                OperatorName = operatorName,
                RequestUri = httpContext.Request.Path.Value,
                RequestMethod = httpContext.Request.Method,
                Ip = httpContext.Connection.RemoteIpAddress?.ToString(),
                Browser = browser,
                Os = os,
                Status = actionExecutedContext.Exception == null ? 1 : 0,
                ErrorMsg = actionExecutedContext.Exception?.Message?.Length > 255
                    ? actionExecutedContext.Exception.Message[..255]
                    : actionExecutedContext.Exception?.Message,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                CreateTime = DateTime.Now,
            };

            _dbContext.SysLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // 日志记录失败不影响主请求
        }
    }

    private static string GetEnumDescription<T>(T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    public static (string Browser, string Os) ParseUserAgent(string ua)
    {
        if (string.IsNullOrEmpty(ua))
            return (string.Empty, string.Empty);

        // OS detection
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

        // Browser detection (order matters - Edge before Chrome)
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
