namespace Youlai.Domain.Entities;

/// <summary>
/// 系统配置实体
/// </summary>
public sealed class SysConfig : IHasAudit
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>配置名称</summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>配置键（唯一）</summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>配置值</summary>
    public string ConfigValue { get; set; } = string.Empty;

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

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
