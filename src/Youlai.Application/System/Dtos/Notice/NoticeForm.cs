using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告表单
/// </summary>
public sealed class NoticeForm
{
    /// <summary>
    /// 公告ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 标题
    /// </summary>
    [Required]
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 内容
    /// </summary>
    [Required]
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
    public IReadOnlyCollection<string>? TargetUserIds { get; init; }

    /// <summary>
    /// 目标类型
    /// </summary>
    [JsonPropertyName("targetType")]
    public int? TargetType { get; init; }
}
