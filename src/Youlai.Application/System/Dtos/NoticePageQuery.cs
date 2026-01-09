namespace Youlai.Application.System.Dtos;

/// <summary>
/// 公告分页查询参数
/// </summary>
public sealed class NoticePageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Title { get; init; }

    public int? PublishStatus { get; init; }

    public int? IsRead { get; init; }
}
