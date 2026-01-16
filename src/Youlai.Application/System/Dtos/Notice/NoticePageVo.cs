using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 鍏憡鍒嗛〉鏁版嵁
/// </summary>
public sealed class NoticePageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("type")]
    public int Type { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("publishStatus")]
    public int PublishStatus { get; init; }

    [JsonPropertyName("isRead")]
    public int IsRead { get; init; }

    [JsonPropertyName("publishTime")]
    public string? PublishTime { get; init; }

    [JsonPropertyName("revokeTime")]
    public string? RevokeTime { get; init; }

    [JsonPropertyName("publisherName")]
    public string? PublisherName { get; init; }

    [JsonPropertyName("targetType")]
    public int? TargetType { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}
