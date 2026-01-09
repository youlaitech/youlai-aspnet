namespace Youlai.Application.Common.Security;

public interface IRolePermsCacheInvalidator
{
    Task InvalidateAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default);
}
