namespace Youlai.Domain.Entities;

public sealed class SysDictItem
{
    public long Id { get; set; }

    public string? DictCode { get; set; }

    public string? Value { get; set; }

    public string? Label { get; set; }

    /// <summary>
    /// 标签样式（el-tag类型）
    /// </summary>
    public string? TagType { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int? Status { get; set; }

    public int? Sort { get; set; }

    public string? Remark { get; set; }

    public DateTime? CreateTime { get; set; }

    public long? CreateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    public long? UpdateBy { get; set; }
}
