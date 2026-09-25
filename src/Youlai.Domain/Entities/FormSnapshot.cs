namespace Youlai.Domain.Entities;

/// <summary>
/// 表单版本快照实体：发布时固化规则，数据回显按提交时版本加载，快照只增不改
/// </summary>
public sealed class FormSnapshot
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>表单定义ID</summary>
    public long FormId { get; set; }

    /// <summary>版本号（发布时递增后的值）</summary>
    public int Version { get; set; }

    /// <summary>表单规则快照（form-create rule 数组 JSON 字符串）</summary>
    public string FormJson { get; set; } = "[]";

    /// <summary>表单全局配置快照 JSON 字符串</summary>
    public string? OptionsJson { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>软删除标记：true=已删除，false=正常</summary>
    public bool IsDeleted { get; set; }
}
