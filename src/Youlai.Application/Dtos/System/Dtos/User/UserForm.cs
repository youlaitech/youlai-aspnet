using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 用户表单
/// </summary>
public sealed class UserForm
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 登录账号
    /// </summary>
    [JsonPropertyName("username")]
    [Required(ErrorMessage = "用户名不能为空")]
    public string? Username { get; init; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    /// <summary>
    /// 手机号
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; init; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("deptId")]
    public long? DeptId { get; init; }

    /// <summary>
    /// 角色ID列表
    /// </summary>
    [JsonPropertyName("roleIds")]
    public IReadOnlyCollection<long>? RoleIds { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "用户状态不正确")]
    public int? Status { get; init; }
}
