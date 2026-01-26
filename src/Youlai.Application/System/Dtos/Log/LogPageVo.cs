using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Log;

/// <summary>
/// 日志分页数据
/// </summary>
public sealed class LogPageVo
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 模块
    /// </summary>
    [JsonPropertyName("module")]
    public string Module { get; init; } = string.Empty;

    /// <summary>
    /// 操作内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 请求地址
    /// </summary>
    [JsonPropertyName("requestUri")]
    public string RequestUri { get; init; } = string.Empty;

    /// <summary>
    /// 请求方法
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// 客户端IP
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    /// <summary>
    /// 地区
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; init; } = string.Empty;

    /// <summary>
    /// 浏览器
    /// </summary>
    [JsonPropertyName("browser")]
    public string Browser { get; init; } = string.Empty;

    /// <summary>
    /// 操作系统
    /// </summary>
    [JsonPropertyName("os")]
    public string Os { get; init; } = string.Empty;

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [JsonPropertyName("executionTime")]
    public long ExecutionTime { get; init; }

    /// <summary>
    /// 操作人
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; init; } = string.Empty;
}
