using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Youlai.Application.Auth;
using Youlai.Application.Auth.Models;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Options;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.Security.Models;

namespace Youlai.Infrastructure.Security;

/// <summary>
/// JWT 令牌管理器（签发、校验、Redis 会话作废）；调用方只依赖 ITokenManager 接口
/// </summary>
internal sealed class JwtTokenManager : ITokenManager
{
    private const string BearerPrefix = "Bearer ";

    private readonly SecurityOptions _securityOptions;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<JwtTokenManager> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenManager(
        IOptions<SecurityOptions> securityOptions,
        IConnectionMultiplexer redis,
        ILogger<JwtTokenManager> logger)
    {
        _securityOptions = securityOptions.Value;
        _redis = redis;
        _logger = logger;

        var secretKey = Encoding.UTF8.GetBytes(_securityOptions.Session.Jwt.SecretKey);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    }

    /// <summary>
    /// 签发访问令牌与刷新令牌对，令牌内的 TokenVersion 取自 Redis 当前版本号
    /// </summary>
    public AuthenticationTokenDto GenerateToken(AuthTokenSubject subject)
    {
        var accessTtl = _securityOptions.Session.AccessTokenTimeToLive;
        var refreshTtl = _securityOptions.Session.RefreshTokenTimeToLive;

        var accessToken = GenerateJwt(subject, accessTtl, isRefreshToken: false);
        var refreshToken = GenerateJwt(subject, refreshTtl, isRefreshToken: true);

        return new AuthenticationTokenDto(
            TokenType: "Bearer",
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: accessTtl
        );
    }

    /// <summary>
    /// 用刷新令牌换新访问令牌，刷新令牌本身复用（不再重新签发）
    /// </summary>
    public AuthenticationTokenDto RefreshToken(string refreshToken)
    {
        if (!ValidateToken(refreshToken, validateRefreshToken: true, out var payload))
        {
            throw new BusinessException(ResultCode.RefreshTokenInvalid);
        }

        var subject = FromPayload(payload);

        var accessTtl = _securityOptions.Session.AccessTokenTimeToLive;
        var newAccessToken = GenerateJwt(subject, accessTtl, isRefreshToken: false);

        return new AuthenticationTokenDto(
            TokenType: "Bearer",
            AccessToken: newAccessToken,
            RefreshToken: refreshToken,
            ExpiresIn: accessTtl
        );
    }

    /// <summary>
    /// 作废当前令牌：将 JTI 写入 Redis 黑名单，TTL 设为令牌剩余有效期，过期自动清理
    /// </summary>
    public async Task InvalidateTokenAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[BearerPrefix.Length..].Trim()
            : authorizationHeader.Trim();

        JwtSecurityToken jwt;
        try
        {
            jwt = _tokenHandler.ReadJwtToken(token);
        }
        catch
        {
            return;
        }

        var jti = jwt.Id;
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expSeconds = jwt.Payload.Expiration;

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Auth.BlacklistToken, jti);

        if (expSeconds.HasValue)
        {
            var ttlSeconds = expSeconds.Value - nowSeconds;
            if (ttlSeconds <= 0)
            {
                return;
            }

            await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(ttlSeconds));
            return;
        }

        await db.StringSetAsync(key, "1");
    }

    /// <summary>
    /// 校验访问令牌签名与版本（非刷新令牌）
    /// </summary>
    public bool ValidateAccessToken(string token)
    {
        return ValidateToken(token, validateRefreshToken: false, out _);
    }

    /// <summary>
    /// 校验访问令牌并解析载荷
    /// </summary>
    public bool TryGetAccessTokenPayload(string token, out JwtPayload payload)
    {
        return ValidateToken(token, validateRefreshToken: false, out payload);
    }

    /// <summary>
    /// 递增用户令牌版本，使该用户所有历史令牌在下次校验时统一失效（踢下线）
    /// </summary>
    public async Task InvalidateUserSessionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return;
        }

        var db = _redis.GetDatabase();
        var versionKey = string.Format(RedisKeyConstants.Auth.UserTokenVersion, userId);
        await db.StringIncrementAsync(versionKey).ConfigureAwait(false);
    }

    /// <summary>
    /// 生成 JWT：写入用户/部门/数据权限/版本号等声明，ttlSeconds=-1 表示永不过期
    /// </summary>
    private string GenerateJwt(AuthTokenSubject subject, int ttlSeconds, bool isRefreshToken)
    {
        var now = DateTimeOffset.UtcNow;
        var jti = Guid.NewGuid().ToString("N");

        var db = _redis.GetDatabase();
        var versionKey = string.Format(RedisKeyConstants.Auth.UserTokenVersion, subject.UserId);
        var currentVersionValue = db.StringGet(versionKey);
        // Token 版本号，用于整体失效历史令牌
        var tokenVersion = currentVersionValue.HasValue && int.TryParse(currentVersionValue.ToString(), out var v) ? v : 0;

        var secretKey = Encoding.UTF8.GetBytes(_securityOptions.Session.Jwt.SecretKey);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

        var payload = new JwtPayload
        {
            { JwtClaimConstants.UserId, subject.UserId },
            { JwtClaimConstants.DeptId, subject.DeptId },
            { JwtClaimConstants.DataScopes, JsonSerializer.Serialize(subject.DataScopes) },
            { JwtClaimConstants.TokenType, isRefreshToken },
            { JwtClaimConstants.TokenVersion, tokenVersion },
            { JwtRegisteredClaimNames.Sub, subject.Username },
            { JwtRegisteredClaimNames.Jti, jti },
            { JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds() },
        };

        payload.Add(JwtClaimConstants.Authorities, subject.Authorities.ToArray());

        if (ttlSeconds != -1)
        {
            // ttlSeconds = -1 时表示不设置过期时间
            payload.Add(JwtRegisteredClaimNames.Exp, now.AddSeconds(ttlSeconds).ToUnixTimeSeconds());
        }

        var header = new JwtHeader(signingCredentials);
        var token = new JwtSecurityToken(header, payload);

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 签名校验通过后，再比 TokenVersion（旧版本失效）和黑名单（登出失效）
    /// </summary>
    private bool ValidateToken(string token, bool validateRefreshToken, out JwtPayload payload)
    {
        payload = new JwtPayload();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("JWT validation failed: token is empty.");
            return false;
        }

        try
        {
            _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt)
            {
                _logger.LogWarning("JWT validation failed: validated token is not JwtSecurityToken.");
                return false;
            }

            payload = jwt.Payload;

            if (validateRefreshToken)
            {
                var isRefreshToken = payload.TryGetValue(JwtClaimConstants.TokenType, out var t)
                    && (t is bool b ? b : bool.TryParse(t?.ToString(), out var parsed) && parsed);

                if (!isRefreshToken)
                {
                    _logger.LogWarning("JWT validation failed: token type mismatch (expected refresh token). JTI={Jti}", jwt.Id);
                    return false;
                }
            }

            var uid = payload.TryGetValue(JwtClaimConstants.UserId, out var uidObj) ? TryParseInt64(uidObj) : 0;
            if (uid != 0)
            {
                var tokenVersionObj = payload.TryGetValue(JwtClaimConstants.TokenVersion, out var tv) ? tv : null;
                var tokenVersion = TryParseInt32(tokenVersionObj);

                // 只要用户 Token 版本号递增，旧 token 统一失效
                var db = _redis.GetDatabase();
                var versionKey = string.Format(RedisKeyConstants.Auth.UserTokenVersion, uid);
                var currentVersionValue = db.StringGet(versionKey);
                var currentVersion = currentVersionValue.HasValue && int.TryParse(currentVersionValue.ToString(), out var cv)
                    ? cv
                    : 0;

                if (tokenVersion < currentVersion)
                {
                    _logger.LogWarning(
                        "JWT validation failed: token version mismatch. UserId={UserId}, TokenVersion={TokenVersion}, CurrentVersion={CurrentVersion}, JTI={Jti}",
                        uid,
                        tokenVersion,
                        currentVersion,
                        jwt.Id);
                    return false;
                }
            }

            var jti = jwt.Id;
            if (!string.IsNullOrWhiteSpace(jti))
            {
                var db = _redis.GetDatabase();
                var blacklistKey = string.Format(RedisKeyConstants.Auth.BlacklistToken, jti);
                // 登出时会把 JTI 写入黑名单
                if (db.KeyExists(blacklistKey))
                {
                    _logger.LogWarning("JWT validation failed: token is blacklisted. JTI={Jti}", jti);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed: token validation threw exception.");
            return false;
        }
    }

    /// <summary>
    /// 从 JWT 载荷还原 token 主体信息
    /// </summary>
    private static AuthTokenSubject FromPayload(JwtPayload payload)
    {
        var userId = TryGetInt64(payload, JwtClaimConstants.UserId);
        var deptId = TryGetInt64(payload, JwtClaimConstants.DeptId);
        var username = payload.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) ? sub?.ToString() ?? string.Empty : string.Empty;

        // dataScopes: 写入时做了 JSON 序列化，读取时需区分 JsonElement 和普通对象
        List<RoleDataScope> dataScopes = new();
        if (payload.TryGetValue(JwtClaimConstants.DataScopes, out var dataScopesObj))
        {
            try
            {
                string? json = dataScopesObj switch
                {
                    JsonElement dsJe => dsJe.ValueKind == JsonValueKind.String ? dsJe.GetString() : dsJe.ToString(),
                    _ => dataScopesObj?.ToString()
                };

                if (!string.IsNullOrWhiteSpace(json))
                {
                    dataScopes = JsonSerializer.Deserialize<List<RoleDataScope>>(json) ?? new();
                }
            }
            catch
            {
                // 反序列化失败则留空
            }
        }

        // authorities: JWT 反序列化后为 JsonElement 数组，不能用 ToString() 否则会拿到原始 JSON 字符串
        var authorities = new List<string>();
        if (payload.TryGetValue(JwtClaimConstants.Authorities, out var authObj))
        {
            switch (authObj)
            {
                case JsonElement je when je.ValueKind == JsonValueKind.Array:
                    foreach (var item in je.EnumerateArray())
                    {
                        var val = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                        if (!string.IsNullOrWhiteSpace(val)) authorities.Add(val);
                    }
                    break;
                case JsonElement je when je.ValueKind == JsonValueKind.String:
                    {
                        var val = je.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) authorities.Add(val);
                    }
                    break;
                case string s when !string.IsNullOrWhiteSpace(s):
                    authorities.Add(s);
                    break;
                case IEnumerable<object> arr:
                    authorities.AddRange(arr.Select(a => a.ToString()).Where(a => !string.IsNullOrWhiteSpace(a))!);
                    break;
                default:
                    {
                        var single = authObj?.ToString();
                        if (!string.IsNullOrWhiteSpace(single)) authorities.Add(single);
                    }
                    break;
            }
        }

        return new AuthTokenSubject(userId, deptId, dataScopes, username, authorities);
    }

    /// <summary>
    /// 从 JWT 声明值解析 long，兼容 JsonElement / long / int / 字符串
    /// </summary>
    private static long TryParseInt64(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt64(out var jeVal))
        {
            return jeVal;
        }

        if (value is long l)
        {
            return l;
        }

        if (value is int i)
        {
            return i;
        }

        return long.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    }

    /// <summary>
    /// 从 JWT 声明值解析 int，兼容 JsonElement / long / int / 字符串
    /// </summary>
    private static int TryParseInt32(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var jeVal))
        {
            return jeVal;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return checked((int)l);
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    }

    private static long TryGetInt64(JwtPayload payload, string key)
    {
        if (!payload.TryGetValue(key, out var obj) || obj is null)
        {
            return 0;
        }

        // JWT 反序列化后数值类型为 JsonElement
        if (obj is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt64(out var jeVal))
        {
            return jeVal;
        }

        if (obj is long l)
        {
            return l;
        }

        if (obj is int i)
        {
            return i;
        }

        if (long.TryParse(obj.ToString(), out var parsed))
        {
            return parsed;
        }

        return 0;
    }
}
