using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
public sealed class DictQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public int? Status { get; init; }
}
