namespace Youlai.Domain.Entities;

/// <summary>
/// 表单数据实体
/// </summary>
public sealed class FormData : IHasAudit
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>表单定义ID</summary>
    public long FormId { get; set; }

    /// <summary>表单版本（提交时快照）</summary>
    public int FormVersion { get; set; }

    /// <summary>表单数据（field -> value 映射的 JSON 字符串）</summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>提交人ID（匿名提交为空）</summary>
    public long? CreateBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>修改人ID</summary>
    public long? UpdateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>软删除标记：true=已删除，false=正常</summary>
    public bool IsDeleted { get; set; }
}
