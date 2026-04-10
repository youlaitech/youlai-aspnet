namespace Youlai.Domain.Entities;

/// <summary>
/// 用户公告阅读记录
/// </summary>
public sealed class SysUserNotice
{
    public long Id { get; set; }

    public long NoticeId { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// 是否已读（0=未读 1=已读）
    /// </summary>
    public int IsRead { get; set; }

    public DateTime? ReadTime { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
