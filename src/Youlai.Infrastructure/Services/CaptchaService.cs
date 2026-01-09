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
            ctx.Fill(Color.White);

            for (var i = 0; i < _options.InterfereCount; i++)
            {
                var p1 = new PointF(RandomNumberGenerator.GetInt32(0, _options.Width), RandomNumberGenerator.GetInt32(0, _options.Height));
                var p2 = new PointF(RandomNumberGenerator.GetInt32(0, _options.Width), RandomNumberGenerator.GetInt32(0, _options.Height));
                ctx.DrawLine(Color.LightGray, 1, p1, p2);
            }

            var family = ResolveFontFamily();
            var font = family.CreateFont(_options.FontSize, FontStyle.Bold);

            var origin = new PointF(10, (_options.Height - _options.FontSize) / 2f);
            var textOptions = new RichTextOptions(font)
            {
                Origin = origin,
            };
            ctx.DrawText(textOptions, code, Color.Black);
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
