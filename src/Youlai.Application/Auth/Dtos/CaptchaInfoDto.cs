namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 验证码信息
/// </summary>
public sealed record CaptchaInfoDto(
    string CaptchaId,
    string CaptchaBase64
);
