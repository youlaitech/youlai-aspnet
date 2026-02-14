namespace Youlai.Domain.Entities;

/// <summary>
/// 角色与部门关联（自定义数据权限）
/// </summary>
public sealed class SysRoleDept
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long DeptId { get; set; }
}
