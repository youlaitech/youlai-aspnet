using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Realtime;
using Youlai.Application.Results;
using Youlai.Application.Security;

namespace Youlai.Api.Controllers.Realtime;

/// <summary>
/// 实时连接票据与在线状态
/// </summary>
[ApiController]
[Route("api/v1/realtime")]
[Tags("13.Realtime Connection")]
public class RealtimeController : ControllerBase
{
    private const string BearerPrefix = "Bearer ";

    private readonly IRealtimeTicketService _tickets;
    private readonly IRealtimeService _sessions;
    private readonly ICurrentUser _currentUser;

    public RealtimeController(IRealtimeTicketService tickets, IRealtimeService sessions, ICurrentUser currentUser)
    {
        _tickets = tickets;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 校验访问凭证并签发一次性握手票据，票据 30 秒内有效
    /// </summary>
    [HttpPost("tickets")]
    [Authorize]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateRealtimeTicketRequest request,
        [FromHeader(Name = "Authorization")] string? authorization,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Unauthorized(Result.Failed(ResultCode.AccessTokenInvalid));
        }

        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(Result.Failed(ResultCode.AccessTokenInvalid));
        }

        var accessToken = authorization[BearerPrefix.Length..].Trim();
        var ticket = await _tickets.CreateAsync(userId.Value, accessToken, request, cancellationToken).ConfigureAwait(false);
        if (ticket is null)
        {
            return Unauthorized(Result.Failed(ResultCode.AccessTokenInvalid));
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(Result.Success(ticket));
    }

    /// <summary>
    /// 查询当前节点在线状态
    /// </summary>
    [HttpGet("presence")]
    [Authorize]
    [HasPerm("sys:online:view")]
    public IActionResult GetPresence()
    {
        var snapshot = _sessions.PresenceSnapshot();

        Response.Headers.CacheControl = "no-store";
        return Ok(Result.Success(snapshot));
    }
}
