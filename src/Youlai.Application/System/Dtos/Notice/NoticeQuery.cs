using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告分页查询参数
/// </summary>
public sealed class NoticeQuery : BaseQuery
{
    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public int? PublishStatus { get; init; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public int? IsRead { get; init; }
}
