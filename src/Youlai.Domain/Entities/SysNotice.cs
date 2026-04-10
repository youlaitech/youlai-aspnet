namespace Youlai.Domain.Entities;

public sealed class SysNotice
{
    public long Id { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    /// <summary>
    /// 类型（1=通知 2=公告）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 级别（L=低 M=中 H=高）
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 目标类型（1=全员 2=指定用户）
    /// </summary>
    public int TargetType { get; set; }

    public string? TargetUserIds { get; set; }

    public long? PublisherId { get; set; }

    /// <summary>
    /// 发布状态（0=未发布 1=已发布 2=已撤回）
    /// </summary>
    public int PublishStatus { get; set; }

    public DateTime? PublishTime { get; set; }

    public DateTime? RevokeTime { get; set; }

    public long CreateBy { get; set; }

    public DateTime CreateTime { get; set; }

    public long? UpdateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
