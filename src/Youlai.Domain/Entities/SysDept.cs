namespace Youlai.Domain.Entities;

/// <summary>
/// 部门实体
/// </summary>
public sealed class SysDept : Youlai.Domain.IHasAudit
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>部门名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>部门编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>父部门 ID（0 表示顶级）</summary>
    public long ParentId { get; set; }

    /// <summary>部门树路径（逗号分隔的祖先 ID，含自身）</summary>
    public string TreePath { get; set; } = string.Empty;

    /// <summary>排序</summary>
    public short? Sort { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int? Status { get; set; }

    /// <summary>创建人 ID</summary>
    public long? CreateBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
