using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 用户表单
/// </summary>
public sealed class UserForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("username")]
    [Required(ErrorMessage = "用户名不能为空")]
    public string? Username { get; init; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("deptId")]
    public long? DeptId { get; init; }

    [JsonPropertyName("roleIds")]
    public IReadOnlyCollection<long>? RoleIds { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "用户状态不正确")]
    public int? Status { get; init; }
}
