namespace Youlai.Domain.Entities;

/// <summary>
/// 角色与菜单关联
/// </summary>
public sealed class SysRoleMenu
{
    public long RoleId { get; init; }

    public long MenuId { get; init; }
}
