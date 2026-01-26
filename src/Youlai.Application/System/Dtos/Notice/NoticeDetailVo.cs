using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告详情
/// </summary>
public sealed class NoticeDetailVo
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
    public string? Title { get; init; }

    /// <summary>
    /// 内容
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>
    /// 类型
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; init; }

    /// <summary>
    /// 等级
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; init; }

    /// <summary>
    /// 发布状态
    /// </summary>
    [JsonPropertyName("publishStatus")]
    public int? PublishStatus { get; init; }

    /// <summary>
    /// 指定用户ID列表
    /// </summary>
    [JsonPropertyName("targetUserIds")]
    public string? TargetUserIds { get; init; }

    /// <summary>
    /// 发布人
    /// </summary>
    [JsonPropertyName("publisherName")]
    public string? PublisherName { get; init; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [JsonPropertyName("publishTime")]
    public string? PublishTime { get; init; }
}
