namespace Youlai.Domain.Entities;

/// <summary>
/// 角色与部门关联（自定义数据权限）
/// </summary>
public sealed class SysRoleDept
{
    public long RoleId { get; set; }

    public long DeptId { get; set; }
}
