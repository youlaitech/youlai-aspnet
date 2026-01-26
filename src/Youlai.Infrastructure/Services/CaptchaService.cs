using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 图形验证码
/// </summary>
internal sealed class CaptchaService : ICaptchaService
{
    private const string CaptchaImageKeyFormat = "captcha:image:{0}";

    private static readonly char[] AllowedChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    private readonly IConnectionMultiplexer _redis;
    private readonly CaptchaOptions _options;

    public CaptchaService(IConnectionMultiplexer redis, IOptions<CaptchaOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    /// <summary>
    /// 生成验证码
    /// </summary>
    public async Task<CaptchaInfoDto> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var code = GenerateCode(_options.Length);
        var captchaId = Guid.NewGuid().ToString("N");

        var imageBytes = RenderPng(code);
        var captchaBase64 = "data:image/png;base64," + Convert.ToBase64String(imageBytes);

        var db = _redis.GetDatabase();
        var key = string.Format(CaptchaImageKeyFormat, captchaId);
        await db.StringSetAsync(key, code, TimeSpan.FromSeconds(_options.ExpireSeconds));

        return new CaptchaInfoDto(captchaId, captchaBase64);
    }

    /// <summary>
    /// 校验验证码
    /// </summary>
    public async Task ValidateAsync(string captchaId, string captchaCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(captchaCode))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        var db = _redis.GetDatabase();
        var key = string.Format(CaptchaImageKeyFormat, captchaId);
        var cached = await db.StringGetAsync(key);

        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        var expected = cached.ToString();
        if (!string.Equals(expected, captchaCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await db.KeyDeleteAsync(key);
    }

    private static string GenerateCode(int length)
    {
        if (length <= 0)
        {
            length = 4;
        }

        Span<char> chars = stackalloc char[length];
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < length; i++)
        {
            chars[i] = AllowedChars[bytes[i] % AllowedChars.Length];
        }

        return new string(chars);
    }

    private byte[] RenderPng(string code)
    {
        using var image = new Image<Rgba32>(_options.Width, _options.Height);

        image.Mutate(ctx =>
        {
            var background = Color.FromRgb(245, 248, 255);
            var lineColor = Color.FromRgb(219, 229, 249);
            ctx.Fill(background);

            for (var i = 0; i < _options.InterfereCount; i++)
            {
                var p1 = new PointF(RandomNumberGenerator.GetInt32(0, _options.Width), RandomNumberGenerator.GetInt32(0, _options.Height));
                var p2 = new PointF(RandomNumberGenerator.GetInt32(0, _options.Width), RandomNumberGenerator.GetInt32(0, _options.Height));
                ctx.DrawLine(lineColor, 1, p1, p2);
            }

            var family = ResolveFontFamily();
            var font = family.CreateFont(_options.FontSize, FontStyle.Bold);
            var palette = new[]
            {
                Color.FromRgb(76, 110, 245),
                Color.FromRgb(34, 139, 230),
                Color.FromRgb(32, 201, 151),
                Color.FromRgb(250, 176, 5),
                Color.FromRgb(255, 146, 43),
            };

            var charSpacing = _options.Width / (code.Length + 1f);
            for (var i = 0; i < code.Length; i++)
            {
                var x = (i + 0.6f) * charSpacing;
                var yOffset = RandomNumberGenerator.GetInt32(-2, 3);
                var origin = new PointF(x, (_options.Height - _options.FontSize) / 2f + yOffset);
                var textOptions = new RichTextOptions(font)
                {
                    Origin = origin,
                };

                var color = palette[RandomNumberGenerator.GetInt32(0, palette.Length)];
                ctx.DrawText(textOptions, code[i].ToString(), color);
            }
        });

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private FontFamily ResolveFontFamily()
    {
        if (!string.IsNullOrWhiteSpace(_options.FontName) && SystemFonts.TryGet(_options.FontName, out var byName))
        {
            return byName;
        }

        if (SystemFonts.TryGet("Arial", out var arial))
        {
            return arial;
        }

        var first = SystemFonts.Families.FirstOrDefault();
        if (first.Name is not null)
        {
            return first;
        }

        throw new InvalidOperationException("No system font family available.");
    }
}
