namespace Youlai.Application.Codegen.Dtos;

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

    /// <summary>
    /// Normalize pagination parameters: ensure PageNum >= 1, PageSize in [1, 200]
    /// </summary>
    public (int PageNum, int PageSize) Normalize()
    {
        var pageNum = PageNum <= 0 ? 1 : PageNum;
        var pageSize = PageSize <= 0 ? 10 : PageSize;
        if (pageSize > 200)
        {
            pageSize = 200;
        }

        return (pageNum, pageSize);
    }
}
