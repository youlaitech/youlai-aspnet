using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 角色表单
/// </summary>
public sealed class RoleForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("name")]
    [Required(ErrorMessage = "角色名称不能为空")]
    public string? Name { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "角色编码不能为空")]
    public string? Code { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "角色状态不正确")]
    public int? Status { get; init; }

    [JsonPropertyName("dataScope")]
    public int? DataScope { get; init; }

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
