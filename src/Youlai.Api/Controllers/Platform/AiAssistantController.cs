using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Ai.Dtos;
using Youlai.Application.Platform.Ai.Services;

namespace Youlai.Api.Controllers.Platform;

/// <summary>
/// AI 鍔╂墜鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵 AI 鍛戒护瑙ｆ瀽涓庢墽琛岃兘鍔?
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
    /// 瑙ｆ瀽鑷劧璇█鍛戒护
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
    /// 鎵ц宸茶В鏋愮殑鍛戒护
    /// </summary>
    [HttpPost("execute")]
    public async Task<Result<AiExecuteResponseDto>> ExecuteCommand(
        [FromBody] AiExecuteRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.ExecuteCommandAsync(request, cancellationToken);
        return Result.Success(result);
    }
}
