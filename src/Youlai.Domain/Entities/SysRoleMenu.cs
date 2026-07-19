namespace Youlai.Domain.Entities;

/// <summary>
/// 角色与菜单关联
/// </summary>
public sealed class SysRoleMenu
{
    /// <summary>角色 ID</summary>
    public long RoleId { get; init; }

    /// <summary>菜单 ID</summary>
    public long MenuId { get; init; }
}
