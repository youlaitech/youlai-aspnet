namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成表分页查询参数
/// </summary>
public sealed class CodegenTableQuery
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageNum { get; init; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }
}
