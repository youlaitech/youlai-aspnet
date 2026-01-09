using Microsoft.AspNetCore.Authorization;

namespace Youlai.Api.Security;

/// <summary>
/// 权限点要求
/// </summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
