namespace Youlai.Application.System.Dtos;

/// <summary>
/// 字典项分页查询参数
/// </summary>
public sealed class DictItemPageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }

    public string? DictCode { get; init; }
}
