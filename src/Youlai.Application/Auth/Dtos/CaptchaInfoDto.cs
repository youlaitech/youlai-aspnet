namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 验证码信息
/// </summary>
/// <param name="CaptchaId">验证码标识</param>
/// <param name="CaptchaBase64">验证码图片（Base64）</param>
public sealed record CaptchaInfoDto(
    string CaptchaId,
    string CaptchaBase64
);
