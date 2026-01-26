using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Ai.Dtos;
using Youlai.Application.Platform.Ai.Services;

namespace Youlai.Api.Controllers.Platform;

/// <summary>
/// AI 助手接口
/// </summary>
/// <remarks>
/// 提供 AI 命令解析与执行能力。
/// </remarks>
[ApiController]
[Route("api/v1/ai/assistant")]
[Authorize]
public sealed class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _aiAssistantService;

    public AiAssistantController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    /// <summary>
    /// 解析自然语言命令
    /// </summary>
    [HttpPost("parse")]
    public async Task<Result<AiParseResponseDto>> ParseCommand(
        [FromBody] AiParseRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.ParseCommandAsync(request, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 执行已解析的命令
    /// </summary>
    [HttpPost("execute")]
    public async Task<Result<AiExecuteResponseDto>> ExecuteCommand(
        [FromBody] AiExecuteRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.ExecuteCommandAsync(request, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 获取 AI 命令记录分页列表
    /// </summary>
    [HttpGet("records")]
    public async Task<PageResult<AiAssistantRecordVo>> GetRecordPage(
        [FromQuery] AiAssistantQuery query,
        CancellationToken cancellationToken)
    {
        return await _aiAssistantService.GetRecordPageAsync(query, cancellationToken);
    }
}
