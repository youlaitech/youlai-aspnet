using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀椤瑰垎椤垫煡璇㈠弬鏁?
/// </summary>
public sealed class DictItemQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public string? DictCode { get; init; }
}
