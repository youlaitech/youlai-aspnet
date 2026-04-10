using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Youlai.Application.Attributes;
using Youlai.Application.Common.Utils;
using Youlai.Domain.Enums;
using Youlai.Domain.Entities;
using Youlai.Application.Persistence;

namespace Youlai.Api.Filters;

/// <summary>
/// Log action filter - records operation logs for actions marked with [Log] attribute
/// </summary>
public sealed class LogActionFilter : IAsyncActionFilter
{
    private readonly IYoulaiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogActionFilter(IYoulaiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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
            var (browser, os) = UserAgentParser.ParseUserAgent(userAgent);

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
            // Log failure should not affect main request
        }
    }

    private static string GetEnumDescription<T>(T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}
