namespace Youlai.Domain.Entities;

/// <summary>
/// 用户公告阅读记录
/// </summary>
public sealed class SysUserNotice
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>公告 ID</summary>
    public long NoticeId { get; set; }

    /// <summary>用户 ID</summary>
    public long UserId { get; set; }

    /// <summary>
    /// 是否已读（0=未读 1=已读）
    /// </summary>
    public int IsRead { get; set; }

    /// <summary>阅读时间</summary>
    public DateTime? ReadTime { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
