using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 部门表单
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
    [Required(ErrorMessage = "父部门ID不能为空")]
    public long? ParentId { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "状态值不正确")]
    public int? Status { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }
}
