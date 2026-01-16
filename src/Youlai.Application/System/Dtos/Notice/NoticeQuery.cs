using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Notice;

/// <summary>
/// 鍏憡鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
public sealed class NoticeQuery : BaseQuery
{
    public string? Title { get; init; }

    public int? PublishStatus { get; init; }

    public int? IsRead { get; init; }
}
