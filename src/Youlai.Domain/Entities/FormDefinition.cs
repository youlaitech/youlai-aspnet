namespace Youlai.Domain.Entities;

/// <summary>
/// 表单定义实体
/// </summary>
public sealed class FormDefinition : IHasAudit
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>表单唯一标识（业务编码，创建后不可修改）</summary>
    public string FormKey { get; set; } = string.Empty;

    /// <summary>表单名称</summary>
    public string FormName { get; set; } = string.Empty;

    /// <summary>表单描述</summary>
    public string? Description { get; set; }

    /// <summary>表单规则（form-create rule 数组 JSON 字符串）</summary>
    public string FormJson { get; set; } = "[]";

    /// <summary>表单全局配置 JSON 字符串</summary>
    public string? OptionsJson { get; set; }

    /// <summary>状态 0草稿 1已发布 -1已停用</summary>
    public int Status { get; set; }

    /// <summary>是否允许匿名公开访问 0否 1是</summary>
    public int IsPublic { get; set; }

    /// <summary>表单类型 normal通用表单 workflow审批表单</summary>
    public string Category { get; set; } = "normal";

    /// <summary>生成的访问菜单ID（NULL未生成，幂等防重复）</summary>
    public long? MenuId { get; set; }

    /// <summary>版本号，每次发布+1</summary>
    public int Version { get; set; } = 1;

    /// <summary>创建人 ID</summary>
    public long? CreateBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdateBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>软删除标记：true=已删除，false=正常</summary>
    public bool IsDeleted { get; set; }
}
