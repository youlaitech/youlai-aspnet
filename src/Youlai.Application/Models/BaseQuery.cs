namespace Youlai.Application.Models;

/// <summary>
/// 分页查询参数
/// </summary>
public abstract class BaseQuery
{
    /// <summary>页码，默认 1</summary>
    public int PageNum { get; set; } = 1;

    /// <summary>每页条数，默认 10</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 规范化分页参数：确保 PageNum >= 1，PageSize 在 [1, 200] 范围内
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
