using System.Text.Json.Serialization;

namespace Youlai.Application.Security.Models;

/// <summary>
/// 用户会话信息（JWT 内的身份、数据权限与角色权限快照）
/// </summary>
public class UserSession
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("deptId")]
    public long? DeptId { get; set; }

    /// <summary>
    /// 数据权限列表（支持多角色）
    /// </summary>
    [JsonPropertyName("dataScopes")]
    public List<RoleDataScope> DataScopes { get; set; } = new();

    /// <summary>
    /// 角色权限集合
    /// </summary>
    [JsonPropertyName("roles")]
    public HashSet<string> Roles { get; set; } = new();
}
