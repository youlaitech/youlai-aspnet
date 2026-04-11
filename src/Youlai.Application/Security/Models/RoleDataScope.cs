using System.Text.Json.Serialization;

namespace Youlai.Application.Security.Models;

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
