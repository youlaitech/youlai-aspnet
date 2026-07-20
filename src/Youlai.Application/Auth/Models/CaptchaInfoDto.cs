namespace Youlai.Application.Auth.Models;

/// <summary>
/// 验证码信息
/// </summary>
/// <param name="CaptchaId">验证码标识</param>
/// <param name="CaptchaBase64">验证码图片（Base64）</param>
/// <param name="CaptchaCode">验证码文本（用于校验）</param>
public sealed record CaptchaInfoDto(
    string CaptchaId,
    string CaptchaBase64,
    string CaptchaCode
);
