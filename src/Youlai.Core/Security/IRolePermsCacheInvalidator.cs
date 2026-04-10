namespace Youlai.Core.Security;

public interface IRolePermsCacheInvalidator
{
    Task InvalidateAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default);
}
