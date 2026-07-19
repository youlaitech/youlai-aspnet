using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;

namespace Youlai.Api.Middlewares;

/// <summary>
/// 全局异常捕获中间件：统一包装未处理异常为 Result
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<JsonOptions> jsonOptions
    )
    {
        _next = next;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    /// <summary>
    /// 全局异常捕获，按优先级包装为统一 Result 响应
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // 异常捕获按优先级从具体到宽泛：业务异常 → 数据库约束冲突 → 其他数据库错误 → 兜底系统异常
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business exception: {Message}", ex.Message);
            await WriteErrorAsync(context, ex.ResultCode, ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true)
        {
            _logger.LogWarning(ex, "Duplicate key violation");
            await WriteErrorAsync(context, ResultCode.IntegrityConstraintViolation, "违反唯一约束");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("denied") == true)
        {
            _logger.LogWarning(ex, "Database access denied");
            await WriteErrorAsync(context, ResultCode.DatabaseAccessDenied, null);
        }
        catch (DbUpdateException ex)
        {
            var innerMsg = ex.InnerException?.Message ?? "N/A";
            _logger.LogError(ex, "Database execution error | Inner: {InnerMessage}", innerMsg);
            await WriteErrorAsync(context, ResultCode.DatabaseExecutionError, $"DB Error: {innerMsg}");
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? "N/A";
            _logger.LogError(ex, "Unhandled exception | Type: {Type} | Inner: {InnerMessage}", ex.GetType().Name, innerMsg);
            await WriteErrorAsync(context, ResultCode.SystemError, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// 按结果码设置 HTTP 状态并写出 JSON；响应已开始则跳过
    /// </summary>
    private async Task WriteErrorAsync(HttpContext context, ResultCode code, string? message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = code.GetHttpStatus();
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = Result.Failed(code, message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
