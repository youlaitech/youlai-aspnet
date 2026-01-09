using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Youlai.Application.Common.Security;

namespace Youlai.Api.Security;

/// <summary>
/// 校验当前用户是否满足权限点
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermissionService _rolePermissionService;

    public PermissionAuthorizationHandler(ICurrentUser currentUser, IRolePermissionService rolePermissionService)
    {
        _currentUser = currentUser;
        _rolePermissionService = rolePermissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (_currentUser.IsRoot)
        {
            context.Succeed(requirement);
            return;
        }

        if (_currentUser.Roles.Count == 0)
        {
            return;
        }

        var cancellationToken = GetCancellationToken(context);
        var rolePerms = await _rolePermissionService.GetRolePermsAsync(_currentUser.Roles, cancellationToken);
        if (rolePerms.Count == 0)
        {
            return;
        }

        var requiredPerm = requirement.Permission;
        var ok = rolePerms.Any(p => SimpleMatch(p, requiredPerm));
        if (ok)
        {
            context.Succeed(requirement);
        }
    }

    private static CancellationToken GetCancellationToken(AuthorizationHandlerContext context)
    {
        if (context.Resource is HttpContext httpContext)
        {
            return httpContext.RequestAborted;
        }

        if (context.Resource is AuthorizationFilterContext filterContext)
        {
            return filterContext.HttpContext.RequestAborted;
        }

        return CancellationToken.None;
    }

    private static bool SimpleMatch(string pattern, string input)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (pattern == "*")
        {
            return true;
        }

        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return string.Equals(pattern, input, StringComparison.Ordinal);
        }

        var parts = pattern.Split('*', StringSplitOptions.None);
        var index = 0;

        if (parts.Length > 0 && parts[0].Length > 0)
        {
            if (!input.StartsWith(parts[0], StringComparison.Ordinal))
            {
                return false;
            }
            index = parts[0].Length;
        }

        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
            {
                continue;
            }

            var next = input.IndexOf(part, index, StringComparison.Ordinal);
            if (next < 0)
            {
                return false;
            }
            index = next + part.Length;
        }

        if (!pattern.EndsWith('*'))
        {
            var last = parts[^1];
            if (last.Length > 0 && !input.EndsWith(last, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
