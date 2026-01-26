using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项分页查询参数
/// </summary>
public sealed class DictItemQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public string? DictCode { get; init; }
}
