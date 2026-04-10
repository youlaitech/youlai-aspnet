using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告分页数据
/// </summary>
public sealed class NoticePageVo
{
    /// <summary>
    /// 公告ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>
    /// 类型
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; init; }

    /// <summary>
    /// 等级
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; init; }

    /// <summary>
    /// 发布状态
    /// </summary>
    [JsonPropertyName("publishStatus")]
    public int PublishStatus { get; init; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [JsonPropertyName("isRead")]
    public int IsRead { get; init; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [JsonPropertyName("publishTime")]
    public string? PublishTime { get; init; }

    /// <summary>
    /// 撤回时间
    /// </summary>
    [JsonPropertyName("revokeTime")]
    public string? RevokeTime { get; init; }

    /// <summary>
    /// 发布人
    /// </summary>
    [JsonPropertyName("publisherName")]
    public string? PublisherName { get; init; }

    /// <summary>
    /// 目标类型
    /// </summary>
    [JsonPropertyName("targetType")]
    public int? TargetType { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}
