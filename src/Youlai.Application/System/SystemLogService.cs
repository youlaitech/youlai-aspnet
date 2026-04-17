using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Youlai.Domain.Entities;
using Youlai.Domain.Enums;
using Youlai.Application.Results;
using Youlai.Application.System.Models.Log;
using Youlai.Application.System.Models.Statistics;
using Youlai.Application.System;
using Youlai.Application.Persistence;
using Youlai.Application.Extensions;

namespace Youlai.Application.System;

internal sealed class SystemLogService : ISystemLogService
{
    private readonly IDbContext _dbContext;

    public SystemLogService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 日志分页列表
    /// </summary>
    public async Task<PageResult<LogPageVo>> GetLogPageAsync(LogQuery query, CancellationToken cancellationToken = default)
    {
        var (pageNum, pageSize) = query.Normalize();

        // 基础查询 + 日期筛选
        var logs = _dbContext.SysLogs.AsNoTracking();

        var (start, end) = ParseDateRange(query.CreateTime);
        if (start.HasValue)
        {
            logs = logs.Where(x => x.CreateTime >= start.Value);
        }
        if (end.HasValue)
        {
            logs = logs.Where(x => x.CreateTime <= end.Value);
        }

        // 按时间倒序，最新的在前面
        logs = logs.OrderByDescending(x => x.CreateTime);

        var pageResult = await logs
            .ToPageAsync(pageNum, pageSize, cancellationToken)
            .ConfigureAwait(false);

        // 枚举转中文、省市拼接等操作放内存里做，不丢性能
        IEnumerable<SysLog> rows = pageResult.Data.List;
        var list = rows.Select(x => new LogPageVo
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
                OperatorId = x.OperatorId == null ? null : x.OperatorId.ToString(),
                OperatorName = x.OperatorName,
                CreateTime = x.CreateTime,
            })
            .ToList();

        return PageResult<LogPageVo>.Success(list, pageResult.Data.Total, pageNum, pageSize);
    }

    /// <summary>
    /// 访问趋势（按天统计 PV / UV）
    /// </summary>
    public async Task<VisitTrendVo> GetVisitTrendAsync(VisitTrendQuery query, CancellationToken cancellationToken = default)
    {
        var startDate = DateOnly.Parse(query.StartDate);
        var endDate = DateOnly.Parse(query.EndDate);

        if (endDate < startDate)
        {
            // 防止用户把起止日期填反了
            (startDate, endDate) = (endDate, startDate);
        }

        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var end = endDate.ToDateTime(TimeOnly.MaxValue);

        // 按天分组统计：日期、PV、UV(去重IP)
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

        // 补齐日期区间，没有数据的日期补 0
        var dates = new List<string>();
        var pvList = new List<int>();
        var uvList = new List<int>();

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dt = d.ToDateTime(TimeOnly.MinValue).Date;
            dates.Add(d.ToString("yyyy-MM-dd"));
            pvList.Add(pvMap.TryGetValue(dt, out var pvCount) ? pvCount : 0);
            uvList.Add(ipMap.TryGetValue(dt, out var ipCount) ? ipCount : 0);
        }

        return new VisitTrendVo
        {
            Dates = dates,
            PvList = pvList,
            UvList = uvList,
        };
    }

    /// <summary>
    /// 访问统计概览（总PV/今日PV/昨日PV、总UV/今日UV/昨日UV、增长率）
    /// </summary>
    public async Task<VisitStatsVo> GetVisitStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var nowTime = now.TimeOfDay;

        // 各维度分别查一次 Count
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

    /// <summary>
    /// 解析前端传来的日期范围参数 [开始, 结束]
    /// </summary>
    private static (DateTime? Start, DateTime? End) ParseDateRange(string?[]? createTime)
    {
        if (createTime is not { Length: >= 1 })
        {
            return (null, null);
        }

        DateTime? start = null;
        DateTime? end = null;

        var startText = createTime[0];
        if (!string.IsNullOrWhiteSpace(startText))
        {
            start = ParseDateTimeMaybeDateOnly(startText.Trim(), isStart: true);
        }

        var endText = createTime.Length >= 2 ? createTime[1] : null;
        if (!string.IsNullOrWhiteSpace(endText))
        {
            end = ParseDateTimeMaybeDateOnly(endText.Trim(), isStart: false);
        }

        return (start, end);
    }

    /// <summary>
    /// 支持纯日期(yyyy-MM-dd) 或完整日期时间格式
    /// </summary>
    private static DateTime? ParseDateTimeMaybeDateOnly(string value, bool isStart)
    {
        // 纯日期格式，自动补上当天 00:00:00 或 23:59:59
        if (value.Length == 10 && DateOnly.TryParse(value, out var d))
        {
            return isStart ? d.ToDateTime(TimeOnly.MinValue) : d.ToDateTime(TimeOnly.MaxValue);
        }

        return DateTime.TryParse(value, out var dt) ? dt : null;
    }

    /// <summary>
    /// 算增长率，昨日为 0 时返回 0 避免除零错误
    /// </summary>
    private static decimal ComputeGrowthRate(int todayCount, int yesterdayCount)
    {
        if (yesterdayCount <= 0)
        {
            return 0m;
        }

        var rate = (decimal)(todayCount - yesterdayCount) / yesterdayCount;
        return Math.Round(rate, 2);
    }

    /// <summary>
    /// 通过 [Display] 特性取枚举的中文名，兜底返回枚举名或"其他"
    /// </summary>
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
