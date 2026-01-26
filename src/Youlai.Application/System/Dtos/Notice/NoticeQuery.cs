using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 公告分页查询参数
/// </summary>
public sealed class NoticeQuery : BaseQuery
{
    public string? Title { get; init; }

    public int? PublishStatus { get; init; }

    public int? IsRead { get; init; }
}
