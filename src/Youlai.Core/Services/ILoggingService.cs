namespace Youlai.Core.Services;

public interface ILoggingService
{
    Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default);
}
