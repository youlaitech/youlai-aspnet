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
using Youlai.Application.Auth.Interfaces;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Options;
using Youlai.Application.Results;

namespace Youlai.Application.Services.Auth;

/// <summary>
/// 图形验证码服务
/// </summary>
internal sealed class CaptchaService : ICaptchaService
{
    private static readonly char[] AllowedChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    private readonly IConnectionMultiplexer _redis;
    private readonly CaptchaOptions _options;

    public CaptchaService(IConnectionMultiplexer redis, IOptions<CaptchaOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<CaptchaInfoDto> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var code = GenerateCode(_options.Length);
        var captchaId = Guid.NewGuid().ToString("N");

        var imageBytes = RenderPng(code);
        var captchaBase64 = "data:image/png;base64," + Convert.ToBase64String(imageBytes);

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.ImageCode, captchaId);
        await db.StringSetAsync(key, code, TimeSpan.FromSeconds(_options.ExpireSeconds)).ConfigureAwait(false);

        return new CaptchaInfoDto(captchaId, captchaBase64);
    }

    public async Task ValidateAsync(string captchaId, string captchaCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(captchaCode))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.ImageCode, captchaId);
        var cached = await db.StringGetAsync(key).ConfigureAwait(false);

        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeTimeout);
        }

        var expected = cached.ToString();
        if (!string.Equals(expected, captchaCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await db.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    private static string GenerateCode(int length)
    {
        var buffer = new byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = AllowedChars[buffer[i] % AllowedChars.Length];
        }

        return new string(chars);
    }

    private byte[] RenderPng(string code)
    {
        using var image = new Image<Rgba32>(_options.Width, _options.Height, new Rgba32(255, 255, 255));

        image.Mutate(ctx =>
        {
            DrawInterferenceLines(ctx, _options.InterfereCount);
            DrawText(ctx, code);
        });

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private void DrawText(IImageProcessingContext ctx, string code)
    {
        var fontName = string.IsNullOrWhiteSpace(_options.FontName) ? SystemFonts.Families.First().Name : _options.FontName;
        var family = SystemFonts.Get(fontName);
        var font = family.CreateFont(_options.FontSize, FontStyle.Bold);

        var color = Color.FromRgb(30, 30, 30);

        var step = _options.Width / (code.Length + 1f);
        for (var i = 0; i < code.Length; i++)
        {
            var x = (i + 0.5f) * step;
            var y = _options.Height / 2f - _options.FontSize / 2f;

            ctx.DrawText(code[i].ToString(), font, color, new PointF(x, y));
        }
    }

    private static void DrawInterferenceLines(IImageProcessingContext ctx, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var rand = Random.Shared;
        for (var i = 0; i < count; i++)
        {
            var color = Color.FromRgb((byte)rand.Next(80, 200), (byte)rand.Next(80, 200), (byte)rand.Next(80, 200));
            var p1 = new PointF(rand.Next(0, 120), rand.Next(0, 40));
            var p2 = new PointF(rand.Next(0, 120), rand.Next(0, 40));
            ctx.DrawLine(color, 1.5f, p1, p2);
        }
    }
}
