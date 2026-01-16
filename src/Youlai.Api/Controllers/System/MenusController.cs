using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Menu;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 鑿滃崟绠＄悊鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鑿滃崟鏍戞煡璇€佽鎯呫€佸垱寤恒€佷慨鏀广€佸垹闄ゅ強璺敱鏋勫缓绛夎兘鍔?
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
    /// 褰撳墠鐢ㄦ埛璺敱
    /// </summary>
    [HttpGet("routes")]
    public async Task<Result<IReadOnlyCollection<RouteVo>>> GetCurrentUserRoutes(CancellationToken cancellationToken)
    {
        var routes = await _menuService.GetCurrentUserRoutesAsync(cancellationToken);
        return Result.Success(routes);
    }

    /// <summary>
    /// 鑿滃崟鍒楄〃
    /// </summary>
    [HttpGet]
    public async Task<Result<IReadOnlyCollection<MenuVo>>> GetMenus([FromQuery] MenuQuery queryParams, CancellationToken cancellationToken)
    {
        var list = await _menuService.GetMenuListAsync(queryParams, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 鑿滃崟涓嬫媺閫夐」
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetMenuOptions([FromQuery] bool onlyParent = false, CancellationToken cancellationToken = default)
    {
        var options = await _menuService.GetMenuOptionsAsync(onlyParent, cancellationToken);
        return Result.Success(options);
    }

    /// <summary>
    /// 鑿滃崟琛ㄥ崟
    /// </summary>
    [HttpGet("{id:long}/form")]
    [HasPerm("sys:menu:update")]
    public async Task<Result<MenuForm>> GetMenuForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _menuService.GetMenuFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 鏂板鑿滃崟
    /// </summary>
    [HttpPost]
    [HasPerm("sys:menu:create")]
    public async Task<Result<object?>> AddMenu([FromBody] MenuForm formData, CancellationToken cancellationToken)
    {
        var ok = await _menuService.SaveMenuAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鏇存柊鑿滃崟
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
    /// 鍒犻櫎鑿滃崟
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPerm("sys:menu:delete")]
    public async Task<Result<object?>> DeleteMenu([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _menuService.DeleteMenuAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 淇敼鍙鐘舵€?
    /// </summary>
    [HttpPatch("{menuId:long}")]
    [HasPerm("sys:menu:update")]
    public async Task<Result<object?>> UpdateMenuVisible([FromRoute] long menuId, [FromQuery] int visible, CancellationToken cancellationToken)
    {
        var ok = await _menuService.UpdateMenuVisibleAsync(menuId, visible, cancellationToken);
        return Result.Judge(ok);
    }
}
