namespace Youlai.Domain.Entities;

public sealed class SysRole
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public int? Sort { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 数据权限范围 1全部 2本部门及子部门 3本部门 4本人
    /// </summary>
    public int? DataScope { get; set; }

    public long? CreateBy { get; set; }

    public DateTime? CreateTime { get; set; }

    public long? UpdateBy { get; set; }

    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记：true=已删除，false=正常
    /// </summary>
    public bool IsDeleted { get; set; }
}
