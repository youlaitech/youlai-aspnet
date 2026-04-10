namespace Youlai.Domain.Entities;

public sealed class SysConfig
{
    public long Id { get; set; }

    public string ConfigName { get; set; } = string.Empty;

    public string ConfigKey { get; set; } = string.Empty;

    public string ConfigValue { get; set; } = string.Empty;

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
