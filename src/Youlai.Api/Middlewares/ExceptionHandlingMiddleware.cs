using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Middlewares;

/// <summary>
/// 统一异常处理并输出 Result 响应
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

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business exception: {Message}", ex.Message);
            // RefreshTokenInvalid 返回 401
            var statusCode = ex.ResultCode == ResultCode.RefreshTokenInvalid
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.BadRequest;
            await WriteErrorAsync(context, statusCode, ex.ResultCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, ResultCode.SystemError, "系统执行出错");
        }
    }

    private async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, ResultCode code, string? message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = Result.Failed(code, message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
