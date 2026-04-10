namespace Youlai.Application.Common.Interfaces;

public interface ILoggingService
{
    Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default);
}
