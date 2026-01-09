namespace Youlai.Domain.Entities;

/// <summary>
/// 用户与角色关联
/// </summary>
public sealed class SysUserRole
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }
}
