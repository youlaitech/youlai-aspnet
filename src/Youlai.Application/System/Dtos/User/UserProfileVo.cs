using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 个人资料数据
/// </summary>
public sealed class UserProfileVo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 登录账号
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

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
    /// 部门名称
    /// </summary>
    [JsonPropertyName("deptName")]
    public string? DeptName { get; init; }

    /// <summary>
    /// 角色名称
    /// </summary>
    [JsonPropertyName("roleNames")]
    public string? RoleNames { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}
