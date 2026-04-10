namespace Youlai.Domain.Entities;

public sealed class SysDict
{
    public long Id { get; set; }

    public string? DictCode { get; set; }

    public string? Name { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int? Status { get; set; }

    public string? Remark { get; set; }

    public DateTime? CreateTime { get; set; }

    public long? CreateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    public long? UpdateBy { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
