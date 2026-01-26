using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Ai.Dtos;

namespace Youlai.Application.Platform.Ai.Services;

/// <summary>
/// AI 助手服务
/// </summary>
public interface IAiAssistantService
{
    Task<AiParseResponseDto> ParseCommandAsync(AiParseRequestDto request, CancellationToken cancellationToken = default);

    Task<AiExecuteResponseDto> ExecuteCommandAsync(AiExecuteRequestDto request, CancellationToken cancellationToken = default);

    Task<PageResult<AiAssistantRecordVo>> GetRecordPageAsync(AiAssistantQuery query, CancellationToken cancellationToken = default);
}
