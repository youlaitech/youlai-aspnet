namespace Youlai.Application.Options;

/// <summary>
/// 验证码配置项
/// </summary>
public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>图片宽度（像素），默认 120</summary>
    public int Width { get; init; } = 120;

    /// <summary>图片高度（像素），默认 40</summary>
    public int Height { get; init; } = 40;

    /// <summary>验证码字符数，默认 4</summary>
    public int Length { get; init; } = 4;

    /// <summary>干扰线数量，默认 2</summary>
    public int InterfereCount { get; init; } = 2;

    /// <summary>验证码有效期（秒），默认 120</summary>
    public int ExpireSeconds { get; init; } = 120;

    /// <summary>字体名，null 用默认字体</summary>
    public string? FontName { get; init; }

    /// <summary>字号，默认 22</summary>
    public float FontSize { get; init; } = 22;
}
