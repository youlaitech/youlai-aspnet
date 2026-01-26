using Youlai.Application.Common.Models;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 助手行为记录查询参数
/// </summary>
public sealed class AiAssistantQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public int? ExecuteStatus { get; init; }

    public long? UserId { get; init; }

    public int? ParseStatus { get; init; }

    public string[]? CreateTime { get; init; }

    public string? FunctionName { get; init; }

    public string? AiProvider { get; init; }

    public string? AiModel { get; init; }
}
