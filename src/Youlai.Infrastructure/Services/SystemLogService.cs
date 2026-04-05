using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Enums;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Log;
using Youlai.Application.System.Dtos.Statistics;
using Youlai.Application.System.Services;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

internal sealed class SystemLogService : ISystemLogService
{
    private readonly YoulaiDbContext _dbContext;

    public SystemLogService(YoulaiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PageResult<LogPageVo>> GetLogPageAsync(LogQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var logs =
            from l in _dbContext.SysLogs.AsNoTracking()
            select new
            {
                l.Id,
                l.Module,
                l.ActionType,
                l.Title,
                l.Content,
                l.Status,
                l.RequestUri,
                l.RequestMethod,
                l.Ip,
                l.Province,
                l.City,
                l.Device,
                l.Browser,
                l.Os,
                l.ExecutionTime,
                l.ErrorMsg,
                l.OperatorId,
                l.OperatorName,
                l.CreateTime,
            };

        var (start, end) = ParseDateRange(query.CreateTime);
        if (start.HasValue)
        {
            logs = logs.Where(x => x.CreateTime >= start.Value);
        }

        if (end.HasValue)
        {
            logs = logs.Where(x => x.CreateTime <= end.Value);
        }

        logs = logs.OrderByDescending(x => x.CreateTime);

        var total = await logs.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<LogPageVo>.Success(Array.Empty<LogPageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;
        var rows = await logs
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows
            .Select(x => new LogPageVo
            {
                Id = x.Id.ToString(),
                Module = GetEnumDisplayName<LogModule>(x.Module),
                ActionType = GetEnumDisplayName<ActionType>(x.ActionType),
                Title = x.Title,
                Content = x.Content,
                Status = x.Status,
                RequestUri = x.RequestUri,
                RequestMethod = x.RequestMethod,
                Ip = x.Ip,
                Region = string.Join(" ", new[] { x.Province, x.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Device = x.Device,
                Browser = x.Browser,
                Os = x.Os,
                ExecutionTime = x.ExecutionTime,
                ErrorMsg = x.ErrorMsg,
                OperatorId = x.OperatorId?.ToString(),
                OperatorName = x.OperatorName,
                CreateTime = x.CreateTime,
            })
            .ToList();

        return PageResult<LogPageVo>.Success(list, total, pageNum, pageSize);
    }

    public async Task<VisitTrendVo> GetVisitTrendAsync(VisitTrendQuery query, CancellationToken cancellationToken = default)
    {
        var startDate = DateOnly.Parse(query.StartDate);
        var endDate = DateOnly.Parse(query.EndDate);

        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var end = endDate.ToDateTime(TimeOnly.MaxValue);

        var grouped = await _dbContext.SysLogs
            .AsNoTracking()
            .Where(x => x.CreateTime >= start && x.CreateTime <= end)
            .GroupBy(x => x.CreateTime!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Pv = g.Count(),
                Ip = g.Select(x => x.Ip).Distinct().Count(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pvMap = grouped.ToDictionary(x => x.Date, x => x.Pv);
        var ipMap = grouped.ToDictionary(x => x.Date, x => x.Ip);

        var dates = new List<string>();
        var pvList = new List<int>();
        var uvList = new List<int>();

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dt = d.ToDateTime(TimeOnly.MinValue).Date;
            dates.Add(d.ToString("yyyy-MM-dd"));
            var pv = pvMap.TryGetValue(dt, out var pvCount) ? pvCount : 0;
            var ip = ipMap.TryGetValue(dt, out var ipCount) ? ipCount : 0;
            pvList.Add(pv);
            uvList.Add(ip);
        }

        return new VisitTrendVo
        {
            Dates = dates,
            PvList = pvList,
            UvList = uvList,
        };
    }

    public async Task<VisitStatsVo> GetVisitStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var nowTime = now.TimeOfDay;

        var totalPv = await _dbContext.SysLogs.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        var todayPv = await _dbContext.SysLogs.AsNoTracking().CountAsync(x => x.CreateTime >= today && x.CreateTime < today.AddDays(1), cancellationToken).ConfigureAwait(false);
        var yesterdayPvToNow = await _dbContext.SysLogs.AsNoTracking().CountAsync(
            x => x.CreateTime >= yesterday && x.CreateTime < yesterday.AddDays(1) && x.CreateTime!.Value.TimeOfDay <= nowTime,
            cancellationToken).ConfigureAwait(false);

        var totalUv = await _dbContext.SysLogs.AsNoTracking().Select(x => x.Ip).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        var todayUv = await _dbContext.SysLogs.AsNoTracking().Where(x => x.CreateTime >= today && x.CreateTime < today.AddDays(1)).Select(x => x.Ip).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        var yesterdayUvToNow = await _dbContext.SysLogs.AsNoTracking().Where(
                x => x.CreateTime >= yesterday && x.CreateTime < yesterday.AddDays(1) && x.CreateTime!.Value.TimeOfDay <= nowTime)
            .Select(x => x.Ip)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var pvGrowth = ComputeGrowthRate(todayPv, yesterdayPvToNow);
        var uvGrowth = ComputeGrowthRate(todayUv, yesterdayUvToNow);

        return new VisitStatsVo
        {
            TodayPvCount = todayPv,
            TotalPvCount = totalPv,
            PvGrowthRate = pvGrowth,
            TodayUvCount = todayUv,
            TotalUvCount = totalUv,
            UvGrowthRate = uvGrowth,
        };
    }

    private static (DateTime? Start, DateTime? End) ParseDateRange(string?[]? createTime)
    {
        if (createTime is not { Length: >= 1 })
        {
            return (null, null);
        }

        DateTime? start = null;
        DateTime? end = null;

        if (!string.IsNullOrWhiteSpace(createTime[0]))
        {
            start = ParseDateTimeMaybeDateOnly(createTime[0].Trim(), isStart: true);
        }

        if (createTime.Length >= 2 && !string.IsNullOrWhiteSpace(createTime[1]))
        {
            end = ParseDateTimeMaybeDateOnly(createTime[1].Trim(), isStart: false);
        }

        return (start, end);
    }

    private static DateTime? ParseDateTimeMaybeDateOnly(string value, bool isStart)
    {
        if (value.Length == 10 && DateOnly.TryParse(value, out var d))
        {
            return isStart ? d.ToDateTime(TimeOnly.MinValue) : d.ToDateTime(TimeOnly.MaxValue);
        }

        return DateTime.TryParse(value, out var dt) ? dt : null;
    }

    private static decimal ComputeGrowthRate(int todayCount, int yesterdayCount)
    {
        if (yesterdayCount <= 0)
        {
            return 0m;
        }

        var rate = (decimal)(todayCount - yesterdayCount) / yesterdayCount;
        return Math.Round(rate, 2);
    }

    private static string? GetEnumDisplayName<T>(int? value) where T : Enum
    {
        if (!value.HasValue)
        {
            return null;
        }

        var enumType = typeof(T);
        var enumName = Enum.GetName(enumType, value.Value);
        if (enumName == null)
        {
            return "其他";
        }

        var field = enumType.GetField(enumName);
        if (field == null)
        {
            return "其他";
        }

        var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name ?? "其他";
    }
}
