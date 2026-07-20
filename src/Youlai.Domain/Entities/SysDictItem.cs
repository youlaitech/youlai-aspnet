namespace Youlai.Domain.Entities;

/// <summary>
/// 字典项实体
/// </summary>
public sealed class SysDictItem : Youlai.Domain.IHasAudit
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>字典编码</summary>
    public string? DictCode { get; set; }

    /// <summary>数据值</summary>
    public string? Value { get; set; }

    /// <summary>显示标签</summary>
    public string? Label { get; set; }

    /// <summary>
    /// 标签样式（el-tag类型）
    /// </summary>
    public string? TagType { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int? Status { get; set; }

    /// <summary>排序</summary>
    public int? Sort { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>创建人 ID</summary>
    public long? CreateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdateBy { get; set; }
}
