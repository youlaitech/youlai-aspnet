using Youlai.Application.Common.Models;
using Youlai.Application.System.Dtos;

namespace Youlai.Application.System.Services;

/// <summary>
/// 部门管理
/// </summary>
public interface ISystemDeptService
{
    /// <summary>
    /// 部门列表
    /// </summary>
    Task<IReadOnlyCollection<DeptVo>> GetDeptListAsync(DeptQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 部门下拉选项
    /// </summary>
    Task<IReadOnlyCollection<Option<long>>> GetDeptOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增部门
    /// </summary>
    Task<long> SaveDeptAsync(DeptForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门表单
    /// </summary>
    Task<DeptForm> GetDeptFormAsync(long deptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门
    /// </summary>
    Task<long> UpdateDeptAsync(long deptId, DeptForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除部门
    /// </summary>
    Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default);
}
