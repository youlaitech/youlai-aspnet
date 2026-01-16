using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 登录、刷新令牌与退出
/// </summary>
internal sealed class AuthService : IAuthService
{
    private readonly YoulaiDbContext _dbContext;
    private readonly ICaptchaService _captchaService;
    private readonly JwtTokenManager _tokenManager;

    public AuthService(YoulaiDbContext dbContext, ICaptchaService captchaService, JwtTokenManager tokenManager)
    {
        _dbContext = dbContext;
        _captchaService = captchaService;
        _tokenManager = tokenManager;
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

        var rolesQuery =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && !r.IsDeleted && r.Status == 1
            select new { r.Code, r.DataScope };

        var roles = await rolesQuery.ToListAsync(cancellationToken);

        var authorities = roles
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => SecurityConstants.RolePrefix + c)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var dataScope = roles
            .Select(r => r.DataScope)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(4)
            .Min();

        var subject = new JwtTokenManager.AuthTokenSubject(
            UserId: user.Id,
            DeptId: user.DeptId ?? 0,
            DataScope: dataScope,
            Username: user.Username ?? string.Empty,
            Authorities: authorities
        );

        return _tokenManager.GenerateToken(subject);
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
}
