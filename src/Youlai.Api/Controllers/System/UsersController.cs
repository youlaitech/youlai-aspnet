using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.User;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 用户管理接口
/// </summary>
/// <remarks>
/// 提供用户的分页查询、详情、创建、修改、删除与状态变更等能力
/// </remarks>
[ApiController]
[Route("api/v1/users")]
[Authorize]
[Tags("02.用户接口")]
public sealed class UsersController : ControllerBase
{
    private readonly ISystemUserService _userService;

    public UsersController(ISystemUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 当前登录用户
    /// </summary>
    [HttpGet("me")]
    public async Task<Result<CurrentUserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var dto = await _userService.GetCurrentUserAsync(cancellationToken);
        return Result.Success(dto);
    }

    /// <summary>
    /// 用户分页
    /// </summary>
    [HttpGet]
    [HasPerm("sys:user:list")]
    public Task<PageResult<UserPageVo>> GetUserPage([FromQuery] UserQuery query, CancellationToken cancellationToken)
    {
        return _userService.GetUserPageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 用户表单
    /// </summary>
    [HttpGet("{id:long}/form")]
    [HasPerm("sys:user:update")]
    public async Task<Result<UserForm>> GetUserForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _userService.GetUserFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    [HttpPost]
    [HasPerm("sys:user:create")]
    public async Task<Result<object?>> CreateUser([FromBody] UserForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.CreateUserAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:user:update")]
    public async Task<Result<object?>> UpdateUser([FromRoute] long id, [FromBody] UserForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.UpdateUserAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:user:delete")]
    public async Task<Result<object?>> DeleteUsers([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _userService.DeleteUsersAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 修改用户状态
    /// </summary>
    [HttpPatch("{id:long}/status")]
    [HasPerm("sys:user:update")]
    public async Task<Result<object?>> UpdateUserStatus([FromRoute] long id, [FromQuery] int status, CancellationToken cancellationToken)
    {
        var ok = await _userService.UpdateUserStatusAsync(id, status, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPut("{id:long}/password/reset")]
    [HasPerm("sys:user:reset-password")]
    public async Task<Result<object?>> ResetPassword([FromRoute] long id, [FromQuery] string password, CancellationToken cancellationToken)
    {
        var ok = await _userService.ResetUserPasswordAsync(id, password, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 下载导入模板
    /// </summary>
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var bytes = await _userService.DownloadUserImportTemplateAsync(cancellationToken);
        return File(bytes.ToArray(), "text/csv", "用户导入模板.csv");
    }

    /// <summary>
    /// 导出用户
    /// </summary>
    [HttpGet("export")]
    [HasPerm("sys:user:export")]
    public async Task<IActionResult> ExportUsers([FromQuery] UserQuery query, CancellationToken cancellationToken)
    {
        var bytes = await _userService.ExportUsersAsync(query, cancellationToken);
        return File(bytes.ToArray(), "text/csv", "用户列表.csv");
    }

    /// <summary>
    /// 导入用户
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [HasPerm("sys:user:import")]
    public async Task<Result<Application.Common.Models.ExcelResult>> ImportUsers(
        [FromQuery] long deptId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _userService.ImportUsersAsync(deptId, stream, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 个人资料
    /// </summary>
    [HttpGet("profile")]
    public async Task<Result<UserProfileVo>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _userService.GetProfileAsync(cancellationToken);
        return Result.Success(profile);
    }

    /// <summary>
    /// 更新个人资料
    /// </summary>
    [HttpPut("profile")]
    public async Task<Result<object?>> UpdateProfile([FromBody] UserProfileForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.UpdateProfileAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("password")]
    public async Task<Result<object?>> ChangePassword([FromBody] PasswordChangeForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.ChangePasswordAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 发送手机验证码
    /// </summary>
    [HttpPost("mobile/code")]
    public async Task<Result<object?>> SendMobileCode([FromQuery] string mobile, CancellationToken cancellationToken)
    {
        var ok = await _userService.SendMobileCodeAsync(mobile, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 绑定或修改手机号
    /// </summary>
    [HttpPut("mobile")]
    public async Task<Result<object?>> BindOrChangeMobile([FromBody] MobileUpdateForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.BindOrChangeMobileAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    [HttpDelete("mobile")]
    public async Task<Result<object?>> UnbindMobile([FromBody] PasswordVerifyForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.UnbindMobileAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    [HttpPost("email/code")]
    public async Task<Result<object?>> SendEmailCode([FromQuery] string email, CancellationToken cancellationToken)
    {
        var ok = await _userService.SendEmailCodeAsync(email, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 绑定或修改邮箱
    /// </summary>
    [HttpPut("email")]
    public async Task<Result<object?>> BindOrChangeEmail([FromBody] EmailUpdateForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.BindOrChangeEmailAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    [HttpDelete("email")]
    public async Task<Result<object?>> UnbindEmail([FromBody] PasswordVerifyForm formData, CancellationToken cancellationToken)
    {
        var ok = await _userService.UnbindEmailAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 用户下拉选项
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Application.Common.Models.Option<long>>>> GetOptions(CancellationToken cancellationToken)
    {
        var list = await _userService.GetUserOptionsAsync(cancellationToken);
        return Result.Success(list);
    }
}
