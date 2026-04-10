using Youlai.Core.Auth.Dtos;

namespace Youlai.Core.Auth.Services;

/// <summary>
/// 验证码服务
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// 生成验证码
    /// </summary>
    Task<CaptchaInfoDto> GenerateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验验证码
    /// </summary>
    Task ValidateAsync(string captchaId, string captchaCode, CancellationToken cancellationToken = default);
}
