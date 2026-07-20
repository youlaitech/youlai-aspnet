using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Youlai.Application.Auth.Models;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.Security.Models;
using Youlai.Domain.Entities;

namespace Youlai.Application.Auth;

/// <summary>
/// 认证服务（登录、注册、Token 刷新）
/// </summary>
internal sealed class AuthService : IAuthService
{
    private static readonly TimeSpan SmsCodeTtl = TimeSpan.FromMinutes(5);

    private readonly IReadOnlyDbContext _readDb;
    private readonly ICaptchaService _captchaService;
    private readonly ITokenManager _tokenManager;
    private readonly IDistributedCache _cache;
    private readonly IPasswordHasher<SysUser> _passwordHasher;

    public AuthService(
        IReadOnlyDbContext readDb,
        ICaptchaService captchaService,
        ITokenManager tokenManager,
        IDistributedCache cache,
        IPasswordHasher<SysUser> passwordHasher)
    {
        _readDb = readDb;
        _captchaService = captchaService;
        _tokenManager = tokenManager;
        _cache = cache;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 登录
    /// </summary>
    public async Task<(AuthenticationTokenDto Token, long UserId)> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await _captchaService.ValidateAsync(request.CaptchaId, request.CaptchaCode, cancellationToken);

        var username = request.Username.Trim();

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Username == username && !u.IsDeleted),
            cancellationToken);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserPasswordError);
        }

        if (user.Status != 1)
        {
            throw new BusinessException(ResultCode.UserLoginException, "账号已禁用");
        }

        if (string.IsNullOrWhiteSpace(user.Password)
            || _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password) != PasswordVerificationResult.Success)
        {
            throw new BusinessException(ResultCode.UserPasswordError);
        }

        var token = await GenerateTokenAsync(user.Id, cancellationToken);
        return (token, user.Id);
    }

    /// <summary>
    /// 发送短信登录验证码
    /// </summary>
    public async Task SendSmsLoginCodeAsync(string mobile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号不能为空");
        }

        var code = "1234";

        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        await _cache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SmsCodeTtl },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    public async Task<(AuthenticationTokenDto Token, long UserId)> LoginBySmsAsync(string mobile, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(code))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号或验证码不能为空");
        }

        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        var cached = await _cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(cached))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached, code, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Mobile == mobile.Trim() && !u.IsDeleted),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        if (user.Status != 1)
        {
            throw new BusinessException(ResultCode.UserLoginException, "账号已被禁用");
        }

        var token = await GenerateTokenAsync(user.Id, cancellationToken);
        return (token, user.Id);
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
    /// 登出，使当前 Token 失效
    /// </summary>
    public Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        return _tokenManager.InvalidateTokenAsync(authorizationHeader);
    }

    /// <summary>
    /// 扫码登录换发会话令牌：按用户 ID 查认证信息并复用既有令牌签发逻辑。
    /// </summary>
    public Task<AuthenticationTokenDto> LoginByQrAsync(long userId, CancellationToken cancellationToken = default) =>
        GenerateTokenAsync(userId, cancellationToken);

    /// <summary>
    /// 按用户启用的角色聚合权限点与数据权限范围，组装载荷后签发令牌
    /// </summary>
    private async Task<AuthenticationTokenDto> GenerateTokenAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new { u.Id, u.Username, u.DeptId }),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        // 查询用户的所有角色及其数据权限
        var rolesQuery =
            from ur in _readDb.Query<SysUserRole>()
            join r in _readDb.Query<SysRole>() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && !r.IsDeleted && r.Status == 1
            select new { r.Id, r.Code, r.DataScope };

        var roles = await _readDb.ToListAsync(rolesQuery, cancellationToken).ConfigureAwait(false);

        // 构建角色权限集合
        var authorities = roles
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => SecurityConstants.RolePrefix + c)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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
                var customDeptIds = await _readDb.ToListAsync(
                    _readDb.Query<SysRoleDept>().Where(rd => rd.RoleId == role.Id).Select(rd => rd.DeptId),
                    cancellationToken).ConfigureAwait(false);

                roleDataScope.CustomDeptIds = customDeptIds;
            }

            dataScopes.Add(roleDataScope);
        }

        var subject = new AuthTokenSubject(
            UserId: user.Id,
            DeptId: user.DeptId ?? 0,
            DataScopes: dataScopes,
            Username: user.Username ?? string.Empty,
            Authorities: authorities
        );

        return _tokenManager.GenerateToken(subject);
    }
}
