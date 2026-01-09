using System.ComponentModel.DataAnnotations;

namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 登录请求参数
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// 验证码标识
    /// </summary>
    [Required]
    public string CaptchaId { get; init; } = string.Empty;

    /// <summary>
    /// 验证码
    /// </summary>
    [Required]
    public string CaptchaCode { get; init; } = string.Empty;
}
