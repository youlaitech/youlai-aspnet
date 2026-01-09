using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;

namespace Youlai.Application.System.Services;

/// <summary>
/// 字典管理
/// </summary>
public interface ISystemDictService
{
    /// <summary>
    /// 分页查询字典
    /// </summary>
    Task<PageResult<DictPageVo>> GetDictPageAsync(DictPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 字典下拉选项
    /// </summary>
    Task<IReadOnlyCollection<Option<string>>> GetDictListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字典表单
    /// </summary>
    Task<DictForm> GetDictFormAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增字典
    /// </summary>
    Task<bool> CreateDictAsync(DictForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典
    /// </summary>
    Task<bool> UpdateDictAsync(long id, DictForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除字典
    /// </summary>
    Task<bool> DeleteDictsAsync(string ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询字典项
    /// </summary>
    Task<PageResult<DictItemPageVo>> GetDictItemPageAsync(string dictCode, DictItemPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 字典项列表
    /// </summary>
    Task<IReadOnlyCollection<DictItemOption>> GetDictItemsAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字典项表单
    /// </summary>
    Task<DictItemForm> GetDictItemFormAsync(string dictCode, long itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增字典项
    /// </summary>
    Task<bool> CreateDictItemAsync(string dictCode, DictItemForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项
    /// </summary>
    Task<bool> UpdateDictItemAsync(string dictCode, long itemId, DictItemForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除字典项
    /// </summary>
    Task<bool> DeleteDictItemsAsync(string dictCode, string ids, CancellationToken cancellationToken = default);
}
