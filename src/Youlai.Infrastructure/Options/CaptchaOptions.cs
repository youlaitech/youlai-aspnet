namespace Youlai.Infrastructure.Options;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public int Width { get; init; } = 120;

    public int Height { get; init; } = 40;

    public int Length { get; init; } = 4;

    public int InterfereCount { get; init; } = 2;

    public int ExpireSeconds { get; init; } = 120;

    public string? FontName { get; init; }

    public float FontSize { get; init; } = 22;
}
