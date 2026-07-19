namespace Youlai.Domain.Entities;

/// <summary>
/// 用户与角色关联
/// </summary>
public sealed class SysUserRole
{
    /// <summary>用户 ID</summary>
    public long UserId { get; set; }

    /// <summary>角色 ID</summary>
    public long RoleId { get; set; }
}
