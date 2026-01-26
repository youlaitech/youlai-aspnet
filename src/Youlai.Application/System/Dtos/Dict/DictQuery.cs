using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典分页查询参数
/// </summary>
public sealed class DictQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public int? Status { get; init; }
}
