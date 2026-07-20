namespace Youlai.Domain.Entities;

/// <summary>
/// 通知公告实体
/// </summary>
public sealed class SysNotice
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>标题</summary>
    public string? Title { get; set; }

    /// <summary>内容</summary>
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

    /// <summary>目标用户 ID 列表（逗号分隔），指定用户时有效</summary>
    public string? TargetUserIds { get; set; }

    /// <summary>发布人 ID</summary>
    public long? PublisherId { get; set; }

    /// <summary>
    /// 发布状态（0=未发布 1=已发布 2=已撤回）
    /// </summary>
    public int PublishStatus { get; set; }

    /// <summary>发布时间</summary>
    public DateTime? PublishTime { get; set; }

    /// <summary>撤回时间</summary>
    public DateTime? RevokeTime { get; set; }

    /// <summary>创建人 ID</summary>
    public long CreateBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
