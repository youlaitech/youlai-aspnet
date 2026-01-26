using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Log;

/// <summary>
/// 日志分页查询参数
/// </summary>
public sealed class LogQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public string[]? CreateTime { get; init; }
}
