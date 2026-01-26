using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dept;

/// <summary>
/// 部门数据
/// </summary>
public sealed class DeptVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }

    [JsonPropertyName("children")]
    public IReadOnlyCollection<DeptVo>? Children { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }

    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
}
