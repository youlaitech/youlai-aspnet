namespace Youlai.Domain.Entities;

public sealed class SysUser
{
    public long Id { get; set; }

    public string? Username { get; set; }

    public string? Nickname { get; set; }

    /// <summary>
    /// 性别 1男 2女 0未知
    /// </summary>
    public int? Gender { get; set; }

    /// <summary>
    /// 密码哈希（BCrypt）
    /// </summary>
    public string? Password { get; set; }

    public long? DeptId { get; set; }

    public string? Avatar { get; set; }

    public string? Mobile { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int Status { get; set; }

    public string? Email { get; set; }

    public DateTime? CreateTime { get; set; }

    public long? CreateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    public long? UpdateBy { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 三方登录OpenId
    /// </summary>
    public string? OpenId { get; set; }
}
