namespace Youlai.Application.Security;

/// <summary>
/// 角色权限缓存失效通知接口
/// </summary>
public interface IRolePermsCacheInvalidator
{
    Task InvalidateAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default);
}
