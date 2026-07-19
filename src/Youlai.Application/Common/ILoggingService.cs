namespace Youlai.Application.Common;

/// <summary>
/// 登录与操作日志写入服务
/// </summary>
public interface ILoggingService
{
    Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default);
}
