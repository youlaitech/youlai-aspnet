using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Youlai.Application.Auth.Constants;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// JWT 令牌管理器（签发、校验与作废访问令牌/刷新令牌）
/// </summary>
/// <remarks>
/// 负责生成与校验 JWT，并结合 Redis 处理会话作废与安全版本控制
/// </remarks>
public sealed class JwtTokenManager
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

    public AuthenticationTokenDto RefreshToken(string refreshToken)
    {
        if (!ValidateToken(refreshToken, validateRefreshToken: true, out var payload))
        {
            throw new BusinessException(ResultCode.RefreshTokenInvalid);
        }

        var subject = AuthTokenSubject.FromPayload(payload);

        var accessTtl = _securityOptions.Session.AccessTokenTimeToLive;
        var newAccessToken = GenerateJwt(subject, accessTtl, isRefreshToken: false);

        return new AuthenticationTokenDto(
            TokenType: "Bearer",
            AccessToken: newAccessToken,
            RefreshToken: refreshToken,
            ExpiresIn: accessTtl
        );
    }

    public async Task InvalidateTokenAsync(string? authorizationHeader)
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

    public bool ValidateAccessToken(string token)
    {
        return ValidateToken(token, validateRefreshToken: false, out _);
    }

    /// <summary>
    /// 校验访问令牌并解析载荷
    /// </summary>
    /// <remarks>
    /// 校验通过时返回访问令牌的载荷数据
    /// </remarks>
    public bool TryGetAccessTokenPayload(string token, out JwtPayload payload)
    {
        return ValidateToken(token, validateRefreshToken: false, out payload);
    }

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
            { JwtClaimConstants.DataScope, subject.DataScope },
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

    private static long TryParseInt64(object? value)
    {
        if (value is null)
        {
            return 0;
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

    private static int TryParseInt32(object? value)
    {
        if (value is null)
        {
            return 0;
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

    public sealed record AuthTokenSubject(long UserId, long DeptId, int DataScope, string Username, IReadOnlyCollection<string> Authorities)
    {
        public static AuthTokenSubject FromPayload(JwtPayload payload)
        {
            var userId = TryGetInt64(payload, JwtClaimConstants.UserId);
            var deptId = TryGetInt64(payload, JwtClaimConstants.DeptId);
            var dataScope = TryGetInt32(payload, JwtClaimConstants.DataScope) ?? 4;
            var username = payload.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) ? sub?.ToString() ?? string.Empty : string.Empty;

            var authorities = new List<string>();
            if (payload.TryGetValue(JwtClaimConstants.Authorities, out var authObj))
            {
                if (authObj is string s)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        authorities.Add(s);
                    }
                }
                else if (authObj is IEnumerable<object> arr)
                {
                    authorities.AddRange(arr.Select(a => a.ToString()).Where(a => !string.IsNullOrWhiteSpace(a))!);
                }
                else
                {
                    var single = authObj?.ToString();
                    if (!string.IsNullOrWhiteSpace(single))
                    {
                        authorities.Add(single);
                    }
                }
            }

            return new AuthTokenSubject(userId, deptId, dataScope, username, authorities);
        }

        private static long TryGetInt64(JwtPayload payload, string key)
        {
            if (!payload.TryGetValue(key, out var obj) || obj is null)
            {
                return 0;
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

        private static int? TryGetInt32(JwtPayload payload, string key)
        {
            if (!payload.TryGetValue(key, out var obj) || obj is null)
            {
                return null;
            }

            if (obj is int i)
            {
                return i;
            }

            if (obj is long l)
            {
                return checked((int)l);
            }

            if (int.TryParse(obj.ToString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
