namespace Youlai.Domain.Entities;

/// <summary>
/// 公告
/// </summary>
public sealed class SysNotice
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 级别
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public int TargetType { get; set; }

    /// <summary>
    /// 指定用户ID列表
    /// </summary>
    public string? TargetUserIds { get; set; }

    /// <summary>
    /// 发布人ID
    /// </summary>
    public long? PublisherId { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public int PublishStatus { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishTime { get; set; }

    /// <summary>
    /// 撤回时间
    /// </summary>
    public DateTime? RevokeTime { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public long CreateBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    public long? UpdateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记
    /// </summary>
    public bool IsDeleted { get; set; }
}
