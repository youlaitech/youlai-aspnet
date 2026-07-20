using Youlai.Application.Security.Models;

namespace Youlai.Application.Security;

/// <summary>
/// JWT 令牌主体信息（用户、部门、数据权限范围等），由业务装配后交给 ITokenManager 签发
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="DeptId">部门 ID</param>
/// <param name="DataScopes">数据权限范围列表</param>
/// <param name="Username">用户名</param>
/// <param name="Authorities">权限标识集合</param>
public sealed record AuthTokenSubject(
    long UserId,
    long DeptId,
    IReadOnlyList<RoleDataScope> DataScopes,
    string Username,
    IReadOnlyCollection<string> Authorities);
