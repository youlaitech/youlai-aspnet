using Youlai.Application.System.Models.Menu;
using Youlai.Application.Models;

namespace Youlai.Application.System;

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
    Task<IReadOnlyCollection<Option<long>>> ListMenuOptionsAsync(IReadOnlyCollection<string>? types, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用 AI 推断菜单的访问路径、权限标识与配套按钮，命名风格参照同级菜单
    /// </summary>
    Task<MenuAiVo> AiFillMenuAsync(MenuAiFillForm form, CancellationToken cancellationToken = default);

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
    Task<bool> RemoveMenuAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改可见状态
    /// </summary>
    Task<bool> UpdateMenuVisibleAsync(long menuId, int visible, CancellationToken cancellationToken = default);
}
