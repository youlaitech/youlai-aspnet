using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 鍏憡琛ㄥ崟
/// </summary>
public sealed class NoticeForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [Required]
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("type")]
    public int? Type { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("publishStatus")]
    public int? PublishStatus { get; init; }

    [JsonPropertyName("targetUserIds")]
    public IReadOnlyCollection<string>? TargetUserIds { get; init; }

    [JsonPropertyName("targetType")]
    public int? TargetType { get; init; }
}
