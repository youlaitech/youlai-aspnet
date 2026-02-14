using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Role;

namespace Youlai.Application.System.Services;

/// <summary>
/// 角色管理
/// </summary>
public interface ISystemRoleService
{
    /// <summary>
    /// 分页查询角色
    /// </summary>
    Task<PageResult<RolePageVo>> GetRolePageAsync(RoleQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色下拉选项
    /// </summary>
    Task<IReadOnlyCollection<Option<long>>> GetRoleOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增或更新角色
    /// </summary>
    Task<bool> SaveRoleAsync(RoleForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色表单
    /// </summary>
    Task<RoleForm> GetRoleFormAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除角色
    /// </summary>
    Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改角色状态
    /// </summary>
    Task<bool> UpdateRoleStatusAsync(long roleId, int status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色已分配的菜单ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetRoleMenuIdsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配菜单
    /// </summary>
    Task AssignMenusToRoleAsync(long roleId, IReadOnlyCollection<long> menuIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<long>> GetRoleDeptIdsAsync(long roleId, CancellationToken cancellationToken = default);

    Task AssignDeptsToRoleAsync(long roleId, IReadOnlyCollection<long> deptIds, CancellationToken cancellationToken = default);
}
