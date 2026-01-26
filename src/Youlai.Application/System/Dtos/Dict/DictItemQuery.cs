using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项分页查询参数
/// </summary>
public sealed class DictItemQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 字典编码
    /// </summary>
    public string? DictCode { get; init; }
}
