namespace Youlai.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public sealed class SysUser
{
    public long Id { get; set; }

    /// <summary>用户名</summary>
    public string? Username { get; set; }

    /// <summary>昵称</summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 性别 1男 2女 0未知
    /// </summary>
    public int? Gender { get; set; }

    /// <summary>
    /// 密码哈希（BCrypt）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>所属部门 ID</summary>
    public long? DeptId { get; set; }

    /// <summary>头像地址</summary>
    public string? Avatar { get; set; }

    /// <summary>手机号</summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int Status { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>创建人 ID</summary>
    public long? CreateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdateBy { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
