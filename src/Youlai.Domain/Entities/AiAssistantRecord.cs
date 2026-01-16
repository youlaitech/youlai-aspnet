namespace Youlai.Domain.Entities;

/// <summary>
/// AI 助手行为记录
/// </summary>
public sealed class AiAssistantRecord
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 原始命令
    /// </summary>
    public string? OriginalCommand { get; set; }

    /// <summary>
    /// AI 供应商
    /// </summary>
    public string? AiProvider { get; set; }

    /// <summary>
    /// AI 模型
    /// </summary>
    public string? AiModel { get; set; }

    /// <summary>
    /// 解析状态 0-失败 1-成功
    /// </summary>
    public int? ParseStatus { get; set; }

    /// <summary>
    /// 解析函数调用列表 JSON
    /// </summary>
    public string? FunctionCalls { get; set; }

    /// <summary>
    /// AI 理解说明
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// 置信度
    /// </summary>
    public decimal? Confidence { get; set; }

    /// <summary>
    /// 解析错误信息
    /// </summary>
    public string? ParseErrorMessage { get; set; }

    /// <summary>
    /// 输入 Token 数量
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// 输出 Token 数量
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// 解析耗时(ms)
    /// </summary>
    public int? ParseDurationMs { get; set; }

    /// <summary>
    /// 执行函数名称
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 执行函数参数 JSON
    /// </summary>
    public string? FunctionArguments { get; set; }

    /// <summary>
    /// 执行状态 0-待执行 1-成功 -1-失败
    /// </summary>
    public int? ExecuteStatus { get; set; }

    /// <summary>
    /// 执行错误信息
    /// </summary>
    public string? ExecuteErrorMessage { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
}
