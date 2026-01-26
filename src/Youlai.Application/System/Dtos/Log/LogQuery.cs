using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Log;

/// <summary>
/// 日志分页查询参数
/// </summary>
public sealed class LogQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 时间区间
    /// </summary>
    public string[]? CreateTime { get; init; }
}
