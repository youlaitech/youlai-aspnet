using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Youlai.Application.Attributes;
using Youlai.Application.Common;
using Youlai.Domain.Enums;
using Youlai.Domain.Entities;
using Youlai.Application.Persistence;

namespace Youlai.Api.Filters;

/// <summary>
/// 日志操作过滤器 - 为标记 [Log] 特性的操作记录操作日志
/// </summary>
public sealed class LogActionFilter : IAsyncActionFilter
{
    private readonly IRepository<SysLog> _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogActionFilter(IRepository<SysLog> logRepository, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 执行操作并记录耗时、操作人、IP、浏览器等信息；无 [Log] 特性则直接放行
    /// </summary>
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

            _logRepository.Add(log);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // 写日志失败不影响主请求
        }
    }

    /// <summary>
    /// 读取枚举值上的 [Display(Name=...)] 文本，没有则返回枚举名
    /// </summary>
    private static string GetEnumDescription<T>(T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
        return attr?.GetName() ?? value.ToString();
    }
}
