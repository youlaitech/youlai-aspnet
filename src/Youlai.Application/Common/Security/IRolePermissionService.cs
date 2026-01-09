namespace Youlai.Application.Common.Security;

/// <summary>
/// 角色权限点查询
/// </summary>
public interface IRolePermissionService
{
    /// <summary>
    /// 获取角色拥有的权限点（支持通配符）
    /// </summary>
    Task<IReadOnlyCollection<string>> GetRolePermsAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default);
}
