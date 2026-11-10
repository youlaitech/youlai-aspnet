using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Youlai.Application.Attributes;
using Youlai.Application.Common;
using Youlai.Application.Results;
// 别名规避歧义：框架已有同名的 Microsoft.AspNetCore.Http.IResult（最小 API 结果类型）
using AppResult = Youlai.Application.Results.IResult;
using Youlai.Domain.Enums;
using Youlai.Domain.Entities;
using Youlai.Application.Persistence;

namespace Youlai.Api.Filters;

/// <summary>
/// 日志操作过滤器 - 为标记 [Log] 特性的操作记录操作日志
/// </summary>
public sealed class LogActionFilter : IAsyncActionFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogActionFilter> _logger;

    public LogActionFilter(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory, ILogger<LogActionFilter> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 执行操作并记录耗时、操作人、IP、浏览器等信息；无 [Log] 特性则直接放行
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logAttr = GetLogAttribute(context.ActionDescriptor);
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

            // 操作人：已认证请求从 JWT 取；认证前置场景（登录/登出）由业务侧塞入 HttpContext.Items
            long? operatorId = null;
            string? operatorName = null;
            var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst("userId");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var uid))
            {
                operatorId = uid;
            }
            else if (httpContext.Items[OperationLogKeys.UserId] is long itemUid)
            {
                operatorId = itemUid;
            }

            // 登录名写入 JWT 的 sub，JwtBearer 默认映射为 NameIdentifier
            var usernameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst("sub");
            if (usernameClaim != null)
            {
                operatorName = usernameClaim.Value;
            }
            else if (httpContext.Items[OperationLogKeys.UserName] is string itemName)
            {
                operatorName = itemName;
            }

            var title = logAttr.Title;
            if (string.IsNullOrEmpty(title))
            {
                // 模块与操作同义时（如 LOGIN/LOGOUT）拼接会得到「登录-登录」，仅取其一
                var moduleDesc = GetEnumDescription(logAttr.Module);
                var actionDesc = GetEnumDescription(logAttr.Value);
                title = moduleDesc == actionDesc ? moduleDesc : $"{moduleDesc}-{actionDesc}";
            }

            // 业务结果三重判定：未捕获异常 / 响应体业务码 / 过滤器短路
            // 为什么不只看 Exception：Result.Failed() 返回 200（权限不足、校验失败等），业务失败也必须记为失败
            var (status, errorMsg) = ResolveStatus(actionExecutedContext);

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
                Status = status,
                ErrorMsg = errorMsg?.Length > 255 ? errorMsg[..255] : errorMsg,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                CreateTime = DateTime.Now,
            };

            using var scope = _scopeFactory.CreateScope();
            var logRepository = scope.ServiceProvider.GetRequiredService<IRepository<SysLog>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            logRepository.Add(log);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入操作日志失败");
        }
    }

    /// <summary>
    /// 判定操作结果：未捕获异常优先，其次响应体业务码，最后看过滤器短路；非 Result 响应默认成功
    /// </summary>
    private static (int Status, string? ErrorMsg) ResolveStatus(ActionExecutedContext context)
    {
        if (context.Exception != null)
        {
            return (0, context.Exception.Message);
        }

        if (context.Result is ObjectResult { Value: AppResult result })
        {
            return result.Code == ResultCode.Success.Code()
                ? (1, null)
                : (0, result.Msg);
        }

        return context.Canceled ? (0, "请求被过滤器短路") : (1, null);
    }

    /// <summary>
    /// 从动作描述符读取 [Log] 特性；EndpointMetadata 不含自定义特性，不能用
    /// </summary>
    private static LogAttribute? GetLogAttribute(ActionDescriptor descriptor)
    {
        if (descriptor is not ControllerActionDescriptor controllerAction)
        {
            return null;
        }

        return controllerAction.MethodInfo.GetCustomAttribute<LogAttribute>(inherit: true);
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
