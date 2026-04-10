using System.Text.Json.Serialization;

namespace Youlai.Core.Security;

/// <summary>
/// 用户会话信息
/// 存储在Token中的用户会话快照，包含用户身份、数据权限和角色权限信息。
/// 用于Redis-Token模式下的会话管理，支持在线用户查询和会话控制。
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
