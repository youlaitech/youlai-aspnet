using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 登录、刷新令牌与退出
/// </summary>
internal sealed class AuthService : IAuthService
{
    private static readonly TimeSpan SmsCodeTtl = TimeSpan.FromMinutes(5);

    private readonly YoulaiDbContext _dbContext;
    private readonly ICaptchaService _captchaService;
    private readonly JwtTokenManager _tokenManager;
    private readonly IConnectionMultiplexer _redis;

    public AuthService(
        YoulaiDbContext dbContext,
        ICaptchaService captchaService,
        JwtTokenManager tokenManager,
        IConnectionMultiplexer redis
    )
    {
        _dbContext = dbContext;
        _captchaService = captchaService;
        _tokenManager = tokenManager;
        _redis = redis;
    }

    /// <summary>
    /// 登录
    /// </summary>
    public async Task<AuthenticationTokenDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await _captchaService.ValidateAsync(request.CaptchaId, request.CaptchaCode, cancellationToken);

        var username = request.Username.Trim();

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserPasswordError);
        }

        if (user.Status != 1)
        {
            throw new BusinessException(ResultCode.UserLoginException, "账号已禁用");
        }

        if (string.IsNullOrWhiteSpace(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            throw new BusinessException(ResultCode.UserPasswordError);
        }

        return await GenerateTokenAsync(user.Id, cancellationToken);
    }

    public async Task SendSmsLoginCodeAsync(string mobile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号不能为空");
        }

        var code = "1234";

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        await db.StringSetAsync(key, code, SmsCodeTtl).ConfigureAwait(false);
    }

    public async Task<AuthenticationTokenDto> LoginBySmsAsync(string mobile, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(code))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号或验证码不能为空");
        }

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        var cached = await db.StringGetAsync(key).ConfigureAwait(false);
        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached.ToString(), code, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await db.KeyDeleteAsync(key).ConfigureAwait(false);

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Mobile == mobile.Trim() && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        if (user.Status != 1)
        {
            throw new BusinessException(ResultCode.UserLoginException, "账号已禁用");
        }

        return await GenerateTokenAsync(user.Id, cancellationToken);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public Task<AuthenticationTokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = _tokenManager.RefreshToken(refreshToken);
        return Task.FromResult(token);
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    public Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        return _tokenManager.InvalidateTokenAsync(authorizationHeader);
    }

    private async Task<AuthenticationTokenDto> GenerateTokenAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Username, u.DeptId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        // 查询用户的所有角色及其数据权限
        var rolesQuery =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && !r.IsDeleted && r.Status == 1
            select new { r.Id, r.Code, r.DataScope };

        var roles = await rolesQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        // 构建角色权限集合
        var authorities = roles
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => SecurityConstants.RolePrefix + c)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // 构建数据权限列表（支持多角色）
        var dataScopes = new List<RoleDataScope>();
        foreach (var role in roles)
        {
            var roleDataScope = new RoleDataScope
            {
                RoleCode = role.Code ?? string.Empty,
                DataScope = role.DataScope ?? 4
            };

            // 如果是自定义部门权限，查询该角色的自定义部门列表
            if (role.DataScope == 5 && role.Id != 0)
            {
                var customDeptIds = await _dbContext.SysRoleDepts
                    .AsNoTracking()
                    .Where(rd => rd.RoleId == role.Id)
                    .Select(rd => rd.DeptId)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                roleDataScope.CustomDeptIds = customDeptIds;
            }

            dataScopes.Add(roleDataScope);
        }

        var subject = new JwtTokenManager.AuthTokenSubject(
            UserId: user.Id,
            DeptId: user.DeptId ?? 0,
            DataScopes: dataScopes,
            Username: user.Username ?? string.Empty,
            Authorities: authorities
        );

        return _tokenManager.GenerateToken(subject);
    }
}
