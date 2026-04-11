using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Youlai.Domain.Enums;
using Youlai.Application.Common;
using Youlai.Application.Common;
using Youlai.Domain.Entities;
using Youlai.Application.Persistence;

namespace Youlai.Application.Common;

internal sealed class LoggingService : ILoggingService
{
    private readonly IDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingService(IDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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
            var (browser, os) = UserAgentParser.ParseUserAgent(userAgent);

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
