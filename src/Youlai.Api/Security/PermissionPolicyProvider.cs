using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Youlai.Application.Common.Security;

namespace Youlai.Api.Security;

/// <summary>
/// 生成 Perm:* 动态授权策略
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(SecurityConstants.PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            var perm = policyName[SecurityConstants.PermissionPolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(perm))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
