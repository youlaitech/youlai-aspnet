using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告详情
/// </summary>
public sealed class NoticeDetailVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("type")]
    public int? Type { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("publishStatus")]
    public int? PublishStatus { get; init; }

    [JsonPropertyName("targetUserIds")]
    public string? TargetUserIds { get; init; }

    [JsonPropertyName("publisherName")]
    public string? PublisherName { get; init; }

    [JsonPropertyName("publishTime")]
    public string? PublishTime { get; init; }
}
