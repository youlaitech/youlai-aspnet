namespace Youlai.Application.Common.Models;

/// <summary>
/// 分页查询参数
/// </summary>
public abstract class BaseQuery
{
    public int PageNum { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
