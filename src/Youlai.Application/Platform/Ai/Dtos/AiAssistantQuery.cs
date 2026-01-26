using Youlai.Application.Common.Models;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 助手行为记录查询参数
/// </summary>
public sealed class AiAssistantQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 执行状态
    /// </summary>
    public int? ExecuteStatus { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; init; }

    /// <summary>
    /// 解析状态
    /// </summary>
    public int? ParseStatus { get; init; }

    /// <summary>
    /// 时间区间
    /// </summary>
    public string[]? CreateTime { get; init; }

    /// <summary>
    /// 函数名称
    /// </summary>
    public string? FunctionName { get; init; }

    /// <summary>
    /// 模型厂商
    /// </summary>
    public string? AiProvider { get; init; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string? AiModel { get; init; }
}
