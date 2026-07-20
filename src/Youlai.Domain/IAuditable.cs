namespace Youlai.Domain;

/// <summary>
/// 标记实体具备创建/更新审计字段，由 AppDbContext 在保存时统一填充
/// </summary>
public interface IHasAudit
{
    /// <summary>创建人 ID</summary>
    long? CreateBy { get; set; }

    /// <summary>创建时间</summary>
    DateTime? CreateTime { get; set; }

    /// <summary>更新人 ID</summary>
    long? UpdateBy { get; set; }

    /// <summary>更新时间</summary>
    DateTime? UpdateTime { get; set; }
}
