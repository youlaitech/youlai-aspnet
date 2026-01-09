namespace Youlai.Domain.Entities;

/// <summary>
/// 角色
/// </summary>
public sealed class SysRole
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 状态 1启用 0禁用
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 数据权限范围 1全部 2本部门及子部门 3本部门 4本人
    /// </summary>
    public int? DataScope { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public long? CreateBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    public long? UpdateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 软删除标记
    /// </summary>
    public bool IsDeleted { get; set; }
}
