namespace Youlai.Application.System.Dtos;

/// <summary>
/// 字典分页查询参数
/// </summary>
public sealed class DictPageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }

    public int? Status { get; init; }
}
