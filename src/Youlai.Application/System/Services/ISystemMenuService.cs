using Youlai.Application.System.Dtos;
using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Services;

/// <summary>
/// 菜单与路由
/// </summary>
public interface ISystemMenuService
{
    /// <summary>
    /// 当前用户路由
    /// </summary>
    Task<IReadOnlyCollection<RouteVo>> GetCurrentUserRoutesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 菜单下拉选项
    /// </summary>
    Task<IReadOnlyCollection<Option<long>>> GetMenuOptionsAsync(bool onlyParent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 菜单列表
    /// </summary>
    Task<IReadOnlyCollection<MenuVo>> GetMenuListAsync(MenuQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 菜单表单
    /// </summary>
    Task<MenuForm> GetMenuFormAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增或更新菜单
    /// </summary>
    Task<bool> SaveMenuAsync(MenuForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除菜单
    /// </summary>
    Task<bool> DeleteMenuAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改可见状态
    /// </summary>
    Task<bool> UpdateMenuVisibleAsync(long menuId, int visible, CancellationToken cancellationToken = default);
}
