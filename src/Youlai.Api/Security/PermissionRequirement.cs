using Microsoft.AspNetCore.Authorization;

namespace Youlai.Api.Security;

/// <summary>
/// 权限点要求
/// </summary>
/// <param name="Permission">所需权限标识</param>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
