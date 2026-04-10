namespace Youlai.Core.Models;

/// <summary>
/// Pagination query parameters
/// </summary>
public abstract class BaseQuery
{
    public int PageNum { get; set; } = 1;

    public int PageSize { get; set; } = 10;

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
