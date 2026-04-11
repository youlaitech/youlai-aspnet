namespace Youlai.Application.Common;

public interface ILoggingService
{
    Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default);
}
