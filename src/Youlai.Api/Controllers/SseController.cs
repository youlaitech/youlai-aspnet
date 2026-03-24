using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Services;

namespace Youlai.Api.Controllers;

/// <summary>
/// SSE 推送接口
/// </summary>
[ApiController]
[Route("api/v1/sse")]
[Tags("13.SSE连接")]
public class SseController : ControllerBase
{
    private readonly ISseService _sseService;
    private readonly ICurrentUser _currentUser;

    public SseController(ISseService sseService, ICurrentUser currentUser)
    {
        _sseService = sseService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// SSE 连接
    /// </summary>
    [HttpGet("connect")]
    [Authorize]
    public async Task Connect(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsync("Unauthorized");
            return;
        }

        // 设置 SSE 响应头
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        var reader = _sseService.CreateConnection(userId, cancellationToken);

        // 发送初始连接事件
        await WriteSseEventAsync("init", "connected", cancellationToken);

        // 持续读取并发送消息
        await foreach (var message in reader.ReadAllAsync(cancellationToken))
        {
            var jsonData = message.Data is null
                ? string.Empty
                : JsonSerializer.Serialize(message.Data);
            await WriteSseEventAsync(message.EventName, jsonData, cancellationToken);
        }
    }

    private async Task WriteSseEventAsync(string eventName, string data, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 获取在线用户数
    /// </summary>
    [HttpGet("online-count")]
    public Result<int> GetOnlineCount()
    {
        var count = _sseService.GetOnlineUserCount();
        return Result.Success(count);
    }
}