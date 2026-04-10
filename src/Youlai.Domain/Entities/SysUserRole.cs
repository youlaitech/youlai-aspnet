namespace Youlai.Domain.Entities;

/// <summary>
/// 用户与角色关联
/// </summary>
public sealed class SysUserRole
{
    public long UserId { get; set; }

    public long RoleId { get; set; }
}
