using Microsoft.AspNetCore.Authorization;
using Youlai.Application.Common.Security;

namespace Youlai.Api.Security;

/// <summary>
/// 接口权限声明（Perm:*）
/// </summary>
public sealed class HasPermAttribute : AuthorizeAttribute
{
    public HasPermAttribute(string perm)
    {
        Policy = SecurityConstants.PermissionPolicyPrefix + perm;
    }
}
