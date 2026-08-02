using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Youlai.Application.Persistence;
using Youlai.Domain.Entities;
using Youlai.Domain.Enums;

namespace Youlai.Application.Common;

/// <summary>
/// 操作日志记录（登录日志等）
/// </summary>
internal sealed class LoggingService : ILoggingService
{
    private readonly IRepository<SysLog> _logRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingService(IRepository<SysLog> logRepo, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _logRepo = logRepo;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 记录登录日志
    /// </summary>
    public async Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "";
            var (browser, os) = UserAgentParser.ParseUserAgent(userAgent);

            var title = GetEnumDisplay(LogModule.LOGIN) + "-" + GetEnumDisplay(ActionType.LOGIN);

            var log = new SysLog
            {
                Module = (int)LogModule.LOGIN,
                ActionType = (int)ActionType.LOGIN,
                Title = title,
                RequestUri = requestUri,
                RequestMethod = "POST",
                Ip = context?.Connection.RemoteIpAddress?.ToString(),
                Browser = browser,
                Os = os,
                Status = 1,
                OperatorId = userId,
                CreateTime = DateTime.Now,
            };

            _logRepo.Add(log);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 日志记录失败不影响主流程
        }
    }

    /// <summary>
    /// 读取枚举 [Display(Name=...)] 文本，没有则返回枚举名
    /// </summary>
    private static string GetEnumDisplay<T>(T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DisplayAttribute>();
        return attr?.GetName() ?? value.ToString();
    }
}
