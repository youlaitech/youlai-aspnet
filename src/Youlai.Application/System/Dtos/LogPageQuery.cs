namespace Youlai.Application.System.Dtos;

/// <summary>
/// 日志分页查询参数
/// </summary>
public sealed class LogPageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }

    public string[]? CreateTime { get; init; }
}
