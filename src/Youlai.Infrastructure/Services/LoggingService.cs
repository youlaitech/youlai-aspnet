using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Youlai.Core.Enums;
using Youlai.Core.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Persistence.DbContext;
using Youlai.Infrastructure.Common.Filters;

namespace Youlai.Infrastructure.Services;

internal sealed class LoggingService : ILoggingService
{
    private readonly YoulaiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingService(YoulaiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "";
            var (browser, os) = LogActionFilter.ParseUserAgent(userAgent);

            var log = new SysLog
            {
                Module = (int)LogModule.LOGIN,
                ActionType = (int)ActionType.LOGIN,
                RequestUri = requestUri,
                RequestMethod = "POST",
                Ip = context?.Connection.RemoteIpAddress?.ToString(),
                Browser = browser,
                Os = os,
                Status = 1,
                OperatorId = userId,
                CreateTime = DateTime.Now,
            };

            _dbContext.SysLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 日志记录失败不影响主流程
        }
    }
}
