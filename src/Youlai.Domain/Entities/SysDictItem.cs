namespace Youlai.Domain.Entities;

/// <summary>
/// 字典项
/// </summary>
public sealed class SysDictItem
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 字典编码
    /// </summary>
    public string? DictCode { get; set; }

    /// <summary>
    /// 字典值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 字典标签
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 标签样式
    /// </summary>
    public string? TagType { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public long? CreateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    public long? UpdateBy { get; set; }
}
