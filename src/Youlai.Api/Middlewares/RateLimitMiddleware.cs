using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Youlai.Api.Security;
using Youlai.Application.Options;
using Youlai.Application.Results;

namespace Youlai.Api.Middlewares;

/// <summary>
/// IP 滑动窗口限流中间件（Redis ZSet Lua）
/// </summary>
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitOptions _options;

    private const string LuaSlidingWindow =
        "local key = KEYS[1] " +
        "local now = tonumber(ARGV[1]) " +
        "local window = tonumber(ARGV[2]) " +
        "local member = ARGV[3] " +
        "redis.call('ZREMRANGEBYSCORE', key, 0, now - window) " +
        "redis.call('ZADD', key, now, member) " +
        "redis.call('PEXPIRE', key, window + 1000) " +
        "return redis.call('ZCARD', key)";

    public RateLimitMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RateLimitMiddleware> logger,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 按 IP + 路径限流，超出阈值返回 429 及重试秒数
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var ipConfig = _options.Ip;
        if (!ipConfig.Enabled)
        {
            await _next(context);
            return;
        }

        var ip = ClientIp.Resolve(context);
        if (string.IsNullOrWhiteSpace(ip))
        {
            await _next(context);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = $"rate_limit:ip:{ip}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var member = Guid.NewGuid().ToString();
            var windowMs = (long)ipConfig.Window.TotalMilliseconds;

            var count = (long)(await db.ScriptEvaluateAsync(
                LuaSlidingWindow,
                new RedisKey[] { key },
                new RedisValue[] { now, windowMs, member })!);

            var windowSec = (int)ipConfig.Window.TotalSeconds;
            var remaining = Math.Max(0, ipConfig.Limit - (int)count);
            var resetAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + windowSec;

            if (count > ipConfig.Limit)
            {
                // 窗口内已超最大请求数，返回 429 并附限流头
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.Headers["X-RateLimit-Limit"] = ipConfig.Limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = resetAt.ToString();
                context.Response.Headers["Retry-After"] = windowSec.ToString();

                var result = Result.Failed(ResultCode.RequestConcurrencyLimitExceeded);
                await context.Response.WriteAsync(JsonSerializer.Serialize(result,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return;
            }

            // 正常放行：在响应开始发送前写入限流状态头
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = ipConfig.Limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = resetAt.ToString();
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "限流检查异常，跳过");
        }

        await _next(context);
    }
}
