using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Log;

/// <summary>
/// 日志分页数据
/// </summary>
public sealed class LogPageVo
{
    /// <summary>日志 ID</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>业务模块</summary>
    [JsonPropertyName("module")]
    public string? Module { get; init; }

    /// <summary>操作类型</summary>
    [JsonPropertyName("actionType")]
    public string? ActionType { get; init; }

    /// <summary>操作标题</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>操作内容</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>操作人 ID</summary>
    [JsonPropertyName("operatorId")]
    public string? OperatorId { get; init; }

    /// <summary>操作人名称</summary>
    [JsonPropertyName("operatorName")]
    public string? OperatorName { get; init; }

    /// <summary>执行状态（1 成功 / 0 失败）</summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>请求地址</summary>
    [JsonPropertyName("requestUri")]
    public string? RequestUri { get; init; }

    /// <summary>请求方法</summary>
    [JsonPropertyName("requestMethod")]
    public string? RequestMethod { get; init; }

    /// <summary>客户端 IP</summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; init; }

    /// <summary>所属地区</summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>设备类型</summary>
    [JsonPropertyName("device")]
    public string? Device { get; init; }

    /// <summary>浏览器</summary>
    [JsonPropertyName("browser")]
    public string? Browser { get; init; }

    /// <summary>操作系统</summary>
    [JsonPropertyName("os")]
    public string? Os { get; init; }

    /// <summary>耗时（毫秒）</summary>
    [JsonPropertyName("executionTime")]
    public int? ExecutionTime { get; init; }

    /// <summary>错误信息</summary>
    [JsonPropertyName("errorMsg")]
    public string? ErrorMsg { get; init; }

    /// <summary>创建时间</summary>
    [JsonPropertyName("createTime")]
    public DateTime? CreateTime { get; init; }
}
