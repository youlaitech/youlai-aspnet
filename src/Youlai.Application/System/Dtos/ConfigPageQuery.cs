namespace Youlai.Application.System.Dtos;

/// <summary>
/// 配置分页查询参数
/// </summary>
public sealed class ConfigPageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }
}
