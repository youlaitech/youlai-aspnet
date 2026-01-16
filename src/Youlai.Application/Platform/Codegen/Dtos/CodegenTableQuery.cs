namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成表分页查询参数
/// </summary>
public sealed class CodegenTableQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }
}
