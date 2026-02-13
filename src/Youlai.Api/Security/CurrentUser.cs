using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Youlai.Application.Auth.Constants;
using Youlai.Application.Common.Security;

namespace Youlai.Api.Security;

/// <summary>
/// 从请求 Claims 读取当前用户信息
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public long? UserId => TryGetInt64(JwtClaimConstants.UserId);

    public long? DeptId => TryGetInt64(JwtClaimConstants.DeptId);

    /// <summary>
    /// 数据权限列表（支持多角色）
    /// </summary>
    public IReadOnlyList<RoleDataScope>? DataScopes
    {
        get
        {
            var dataScopesClaim = Principal?.FindFirst(JwtClaimConstants.DataScopes);
            if (string.IsNullOrWhiteSpace(dataScopesClaim?.Value))
            {
                return null;
            }

            try
            {
                var scopes = JsonSerializer.Deserialize<List<RoleDataScope>>(dataScopesClaim.Value);
                return scopes?.AsReadOnly();
            }
            catch
            {
                return null;
            }
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var claims = Principal?.FindAll(JwtClaimConstants.Authorities) ?? Enumerable.Empty<Claim>();
            return claims
                .Select(c => c.Value)
                .Where(v => v.StartsWith(SecurityConstants.RolePrefix, StringComparison.Ordinal))
                .Select(v => v[SecurityConstants.RolePrefix.Length..])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    public bool IsRoot => Roles.Contains(SecurityConstants.RootRoleCode, StringComparer.Ordinal);

    private long? TryGetInt64(string claimType)
    {
        var value = Principal?.FindFirstValue(claimType);
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private int? TryGetInt32(string claimType)
    {
        var value = Principal?.FindFirstValue(claimType);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
