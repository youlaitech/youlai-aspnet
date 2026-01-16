using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dept;

/// <summary>
/// 閮ㄩ棬琛ㄥ崟
/// </summary>
public sealed class DeptForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("parentId")]
    [Required(ErrorMessage = "鐖堕儴闂↖D涓嶈兘涓虹┖")]
    public long? ParentId { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "鐘舵€佸€间笉姝ｇ‘")]
    public int? Status { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }
}
