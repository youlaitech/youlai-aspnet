using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Middlewares;

/// <summary>
/// IP 限流中间件
/// 基于 Redis 固定窗口计数器实现，对齐 youlai-boot RateLimiterFilter
/// 默认限制同一 IP 每秒最多 10 次请求
/// </summary>
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const int DefaultIpLimit = 10;
    private const int RateLimitWindowSec = 1;
    private const string KeyPrefix = "rate_limiter:ip:";

    public RateLimitMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task Invoke(HttpContext context)
    {
        if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var ip = GetClientIp(context);
        if (string.IsNullOrWhiteSpace(ip))
        {
            await _next(context);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}{ip}";

            var count = db.StringIncrement(key);
            if (count == 1)
            {
                db.KeyExpire(key, TimeSpan.FromSeconds(RateLimitWindowSec));
            }

            if (count > DefaultIpLimit)
            {
                await WriteErrorAsync(context);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis 限流检查异常，跳过限流");
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "";
    }

    private async Task WriteErrorAsync(HttpContext context)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = Result.Failed(ResultCode.RequestConcurrencyLimitExceeded);
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
