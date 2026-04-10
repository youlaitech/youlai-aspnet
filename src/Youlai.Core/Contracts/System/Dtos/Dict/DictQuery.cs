using Youlai.Core.Models;

namespace Youlai.Core.System.Dtos.Dict;

/// <summary>
/// 字典分页查询参数
/// </summary>
public sealed class DictQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 状态
    /// </summary>
    public int? Status { get; init; }
}
