using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers;

/// <summary>
/// 菜单管理接口
/// </summary>
/// <remarks>
/// 提供菜单树查询、详情、创建、修改、删除及路由构建等能力
/// </remarks>
[ApiController]
[Route("api/v1/menus")]
[Authorize]
public sealed class MenusController : ControllerBase
{
    private readonly ISystemMenuService _menuService;

    public MenusController(ISystemMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// 当前用户路由
    /// </summary>
    [HttpGet("routes")]
    public async Task<Result<IReadOnlyCollection<RouteVo>>> GetCurrentUserRoutes(CancellationToken cancellationToken)
    {
        var routes = await _menuService.GetCurrentUserRoutesAsync(cancellationToken);
        return Result.Success(routes);
    }

    /// <summary>
    /// 菜单列表
    /// </summary>
    [HttpGet]
    public async Task<Result<IReadOnlyCollection<MenuVo>>> GetMenus([FromQuery] MenuQuery queryParams, CancellationToken cancellationToken)
    {
        var list = await _menuService.GetMenuListAsync(queryParams, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 菜单下拉选项
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetMenuOptions([FromQuery] bool onlyParent = false, CancellationToken cancellationToken = default)
    {
        var options = await _menuService.GetMenuOptionsAsync(onlyParent, cancellationToken);
        return Result.Success(options);
    }

    /// <summary>
    /// 菜单表单
    /// </summary>
    [HttpGet("{id:long}/form")]
    [HasPerm("sys:menu:update")]
    public async Task<Result<MenuForm>> GetMenuForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _menuService.GetMenuFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 新增菜单
    /// </summary>
    [HttpPost]
    [HasPerm("sys:menu:create")]
    public async Task<Result<object?>> AddMenu([FromBody] MenuForm formData, CancellationToken cancellationToken)
    {
        var ok = await _menuService.SaveMenuAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:menu:update")]
    public async Task<Result<object?>> UpdateMenu([FromRoute] long id, [FromBody] MenuForm formData, CancellationToken cancellationToken)
    {
        var ok = await _menuService.SaveMenuAsync(new MenuForm
        {
            Id = id,
            ParentId = formData.ParentId,
            Name = formData.Name,
            Type = formData.Type,
            RouteName = formData.RouteName,
            RoutePath = formData.RoutePath,
            Component = formData.Component,
            Perm = formData.Perm,
            Visible = formData.Visible,
            Sort = formData.Sort,
            Icon = formData.Icon,
            Redirect = formData.Redirect,
            KeepAlive = formData.KeepAlive,
            AlwaysShow = formData.AlwaysShow,
            Params = formData.Params,
        }, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPerm("sys:menu:delete")]
    public async Task<Result<object?>> DeleteMenu([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _menuService.DeleteMenuAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 修改可见状态
    /// </summary>
    [HttpPatch("{menuId:long}")]
    [HasPerm("sys:menu:update")]
    public async Task<Result<object?>> UpdateMenuVisible([FromRoute] long menuId, [FromQuery] int visible, CancellationToken cancellationToken)
    {
        var ok = await _menuService.UpdateMenuVisibleAsync(menuId, visible, cancellationToken);
        return Result.Judge(ok);
    }
}
