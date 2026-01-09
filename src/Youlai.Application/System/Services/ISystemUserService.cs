using Youlai.Application.Common.Results;
using Youlai.Application.Common.Models;
using Youlai.Application.System.Dtos;

namespace Youlai.Application.System.Services;

/// <summary>
/// 用户管理
/// </summary>
public interface ISystemUserService
{
    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询用户
    /// </summary>
    Task<PageResult<UserPageVo>> GetUserPageAsync(UserPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户表单
    /// </summary>
    Task<UserForm> GetUserFormAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增用户
    /// </summary>
    Task<bool> CreateUserAsync(UserForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<bool> UpdateUserAsync(long userId, UserForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<bool> DeleteUsersAsync(string ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改用户状态
    /// </summary>
    Task<bool> UpdateUserStatusAsync(long userId, int status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置密码
    /// </summary>
    Task<bool> ResetUserPasswordAsync(long userId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载导入模板
    /// </summary>
    Task<IReadOnlyCollection<byte>> DownloadUserImportTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出用户
    /// </summary>
    Task<IReadOnlyCollection<byte>> ExportUsersAsync(UserPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导入用户
    /// </summary>
    Task<ExcelResult> ImportUsersAsync(long deptId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取个人资料
    /// </summary>
    Task<UserProfileVo> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新个人资料
    /// </summary>
    Task<bool> UpdateProfileAsync(UserProfileForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(PasswordChangeForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送手机验证码
    /// </summary>
    Task<bool> SendMobileCodeAsync(string mobile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 绑定或修改手机号
    /// </summary>
    Task<bool> BindOrChangeMobileAsync(MobileUpdateForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    Task<bool> SendEmailCodeAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 绑定或修改邮箱
    /// </summary>
    Task<bool> BindOrChangeEmailAsync(EmailUpdateForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户下拉选项
    /// </summary>
    Task<IReadOnlyCollection<Option<long>>> GetUserOptionsAsync(CancellationToken cancellationToken = default);
}
