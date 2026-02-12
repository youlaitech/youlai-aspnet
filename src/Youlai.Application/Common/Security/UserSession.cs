using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Security;

/// <summary>
/// 角色数据权限信息
/// 用于存储单个角色的数据权限范围信息，支持多角色数据权限合并（并集策略）
/// </summary>
public class RoleDataScope
{
    /// <summary>
    /// 角色编码
    /// </summary>
    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 数据权限范围值
    /// 1-所有数据 2-部门及子部门 3-本部门 4-本人 5-自定义部门
    /// </summary>
    [JsonPropertyName("dataScope")]
    public int DataScope { get; set; }

    /// <summary>
    /// 自定义部门ID列表（仅当 dataScope=5 时有效）
    /// </summary>
    [JsonPropertyName("customDeptIds")]
    public List<long>? CustomDeptIds { get; set; }

    /// <summary>
    /// 创建"全部数据"权限
    /// </summary>
    public static RoleDataScope All(string roleCode) => new()
    {
        RoleCode = roleCode,
        DataScope = 1,
        CustomDeptIds = null
    };

    /// <summary>
    /// 创建"部门及子部门"权限
    /// </summary>
    public static RoleDataScope DeptAndSub(string roleCode) => new()
    {
        RoleCode = roleCode,
        DataScope = 2,
        CustomDeptIds = null
    };

    /// <summary>
    /// 创建"本部门"权限
    /// </summary>
    public static RoleDataScope Dept(string roleCode) => new()
    {
        RoleCode = roleCode,
        DataScope = 3,
        CustomDeptIds = null
    };

    /// <summary>
    /// 创建"本人"权限
    /// </summary>
    public static RoleDataScope Self(string roleCode) => new()
    {
        RoleCode = roleCode,
        DataScope = 4,
        CustomDeptIds = null
    };

    /// <summary>
    /// 创建"自定义部门"权限
    /// </summary>
    public static RoleDataScope Custom(string roleCode, List<long> deptIds) => new()
    {
        RoleCode = roleCode,
        DataScope = 5,
        CustomDeptIds = deptIds
    };
}

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

/// <summary>
/// 在线用户信息DTO
/// 用于返回在线用户的基本信息，包括用户名、会话数量和登录时间。
/// </summary>
public class OnlineUserDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 会话数量（多设备登录时大于1）
    /// </summary>
    [JsonPropertyName("sessionCount")]
    public int SessionCount { get; set; }

    /// <summary>
    /// 最早登录时间
    /// </summary>
    [JsonPropertyName("loginTime")]
    public long LoginTime { get; set; }
}
