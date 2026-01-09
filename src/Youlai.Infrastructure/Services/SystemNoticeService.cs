using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Services;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Data;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 公告服务
/// </summary>
internal sealed class SystemNoticeService : ISystemNoticeService
{
    private const int PublishStatusDraft = 0;
    private const int PublishStatusPublished = 1;
    private const int PublishStatusRevoked = -1;

    private const int TargetAll = 1;
    private const int TargetSpecified = 2;

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IWebSocketService _webSocketService;

    public SystemNoticeService(AppDbContext dbContext, ICurrentUser currentUser, IWebSocketService webSocketService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _webSocketService = webSocketService;
    }

    /// <summary>
    /// 公告分页
    /// </summary>
    public async Task<PageResult<NoticePageVo>> GetNoticePageAsync(NoticePageQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var notices = _dbContext.SysNotices.AsNoTracking().Where(n => !n.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var title = query.Title.Trim();
            notices = notices.Where(n => n.Title != null && n.Title.Contains(title));
        }

        if (query.PublishStatus.HasValue)
        {
            notices = notices.Where(n => n.PublishStatus == query.PublishStatus.Value);
        }

        notices = notices.OrderByDescending(n => n.Id);

        var total = await notices.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;

        var rowsQuery =
            from n in notices
            join u in _dbContext.SysUsers.AsNoTracking() on n.PublisherId equals u.Id into pubJoin
            from u in pubJoin.DefaultIfEmpty()
            select new
            {
                n.Id,
                n.Title,
                n.Content,
                n.Type,
                n.Level,
                n.TargetType,
                n.PublishStatus,
                n.PublishTime,
                n.RevokeTime,
                n.CreateTime,
                PublisherName = u != null ? (u.Nickname ?? u.Username) : null,
            };

        var rows = await rowsQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows
            .Select(r => new NoticePageVo
            {
                Id = r.Id,
                Title = r.Title ?? string.Empty,
                Content = r.Content,
                Type = r.Type,
                Level = r.Level,
                PublishStatus = r.PublishStatus,
                IsRead = 0,
                PublishTime = r.PublishTime.HasValue ? r.PublishTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
                RevokeTime = r.RevokeTime.HasValue ? r.RevokeTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
                PublisherName = r.PublisherName,
                TargetType = r.TargetType,
                CreateTime = r.CreateTime.ToString("yyyy-MM-dd HH:mm"),
            })
            .ToArray();

        return PageResult<NoticePageVo>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 公告表单
    /// </summary>
    public async Task<NoticeForm> GetNoticeFormAsync(long id, CancellationToken cancellationToken = default)
    {
        var notice = await _dbContext.SysNotices
            .AsNoTracking()
            .Where(n => n.Id == id && !n.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (notice is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        return new NoticeForm
        {
            Id = notice.Id,
            Title = notice.Title,
            Content = notice.Content,
            Type = notice.Type,
            Level = notice.Level,
            PublishStatus = notice.PublishStatus,
            TargetType = notice.TargetType,
            TargetUserIds = SplitIds(notice.TargetUserIds),
        };
    }

    /// <summary>
    /// 新增公告
    /// </summary>
    public async Task<bool> CreateNoticeAsync(NoticeForm formData, CancellationToken cancellationToken = default)
    {
        ValidateTargets(formData);

        var now = DateTime.UtcNow;
        var uid = GetRequiredCurrentUserId();

        var notice = new SysNotice
        {
            Title = formData.Title?.Trim(),
            Content = formData.Content,
            Type = formData.Type ?? 0,
            Level = formData.Level,
            TargetType = formData.TargetType ?? TargetAll,
            TargetUserIds = JoinIds(formData.TargetUserIds),
            PublishStatus = PublishStatusDraft,
            PublisherId = null,
            PublishTime = null,
            RevokeTime = null,
            CreateBy = uid,
            CreateTime = now,
            UpdateBy = uid,
            UpdateTime = now,
            IsDeleted = false,
        };

        _dbContext.SysNotices.Add(notice);
        return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 更新公告
    /// </summary>
    public async Task<bool> UpdateNoticeAsync(long id, NoticeForm formData, CancellationToken cancellationToken = default)
    {
        ValidateTargets(formData);

        var notice = await _dbContext.SysNotices
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (notice is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        if (notice.PublishStatus == PublishStatusPublished)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "已发布的通知公告不允许编辑");
        }

        notice.Title = formData.Title?.Trim();
        notice.Content = formData.Content;
        notice.Type = formData.Type ?? notice.Type;
        notice.Level = formData.Level;
        notice.TargetType = formData.TargetType ?? notice.TargetType;
        notice.TargetUserIds = JoinIds(formData.TargetUserIds);
        notice.UpdateBy = GetRequiredCurrentUserId();
        notice.UpdateTime = DateTime.UtcNow;

        return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 批量删除公告
    /// </summary>
    public async Task<bool> DeleteNoticesAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = ParseIdList(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的通知公告数据为空");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SysNotices
            .Where(n => idList.Contains(n.Id) && !n.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.UpdateBy, GetRequiredCurrentUserId())
                .SetProperty(x => x.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        await _dbContext.SysUserNotices
            .Where(un => idList.Contains(un.NoticeId) && !un.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDeleted, true), cancellationToken)
            .ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 发布公告
    /// </summary>
    public async Task<bool> PublishNoticeAsync(long id, CancellationToken cancellationToken = default)
    {
        var notice = await _dbContext.SysNotices
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (notice is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        if (notice.PublishStatus == PublishStatusPublished)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告已发布");
        }

        if (notice.TargetType == TargetSpecified && string.IsNullOrWhiteSpace(notice.TargetUserIds))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "推送指定用户不能为空");
        }

        var now = DateTime.UtcNow;
        var uid = GetRequiredCurrentUserId();

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        notice.PublishStatus = PublishStatusPublished;
        notice.PublisherId = uid;
        notice.PublishTime = now;
        notice.RevokeTime = null;
        notice.UpdateBy = uid;
        notice.UpdateTime = now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SysUserNotices
            .Where(un => un.NoticeId == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        var targetUserIds = notice.TargetType == TargetSpecified
            ? ParseStringIdList(notice.TargetUserIds)
            : new HashSet<long>();

        var usersQuery = _dbContext.SysUsers.AsNoTracking().Where(u => !u.IsDeleted && u.Status == 1);
        if (notice.TargetType == TargetSpecified)
        {
            usersQuery = usersQuery.Where(u => targetUserIds.Contains(u.Id));
        }

        var users = await usersQuery
            .Select(u => new { u.Id })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userNotices = users
            .Select(u => new SysUserNotice
            {
                NoticeId = id,
                UserId = u.Id,
                IsRead = 0,
                CreateTime = now,
                UpdateTime = now,
                IsDeleted = false,
            })
            .ToArray();

        if (userNotices.Length > 0)
        {
            _dbContext.SysUserNotices.AddRange(userNotices);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        var payload = new
        {
            id,
            title = notice.Title,
            type = notice.Type,
            publishTime = notice.PublishTime.HasValue ? notice.PublishTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
        };

        foreach (var u in users)
        {
            await _webSocketService.SendUserMessageAsync(u.Id, payload, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 撤回公告
    /// </summary>
    public async Task<bool> RevokeNoticeAsync(long id, CancellationToken cancellationToken = default)
    {
        var notice = await _dbContext.SysNotices
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (notice is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        if (notice.PublishStatus != PublishStatusPublished)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告未发布或已撤回");
        }

        var now = DateTime.UtcNow;
        var uid = GetRequiredCurrentUserId();

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        notice.PublishStatus = PublishStatusRevoked;
        notice.RevokeTime = now;
        notice.UpdateBy = uid;
        notice.UpdateTime = now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SysUserNotices
            .Where(un => un.NoticeId == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 公告详情
    /// </summary>
    public async Task<NoticeDetailVo> GetNoticeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        var noticeQuery =
            from n in _dbContext.SysNotices.AsNoTracking()
            join u in _dbContext.SysUsers.AsNoTracking() on n.PublisherId equals u.Id into pubJoin
            from u in pubJoin.DefaultIfEmpty()
            where n.Id == id && !n.IsDeleted
            select new
            {
                n.Id,
                n.Title,
                n.Content,
                n.Type,
                n.Level,
                n.PublishStatus,
                n.TargetUserIds,
                PublisherName = u != null ? (u.Nickname ?? u.Username) : null,
                n.PublishTime,
            };

        var row = await noticeQuery.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        var userId = _currentUser.UserId;
        if (userId.HasValue && userId.Value > 0)
        {
            await _dbContext.SysUserNotices
                .Where(un => un.NoticeId == id && un.UserId == userId.Value && un.IsRead == 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRead, 1)
                    .SetProperty(x => x.ReadTime, DateTime.UtcNow)
                    .SetProperty(x => x.UpdateTime, DateTime.UtcNow), cancellationToken)
                .ConfigureAwait(false);
        }

        return new NoticeDetailVo
        {
            Id = row.Id,
            Title = row.Title,
            Content = row.Content,
            Type = row.Type,
            Level = row.Level,
            PublishStatus = row.PublishStatus,
            TargetUserIds = row.TargetUserIds,
            PublisherName = row.PublisherName,
            PublishTime = row.PublishTime.HasValue ? row.PublishTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
        };
    }

    /// <summary>
    /// 全部标记已读
    /// </summary>
    public async Task<bool> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();
        var now = DateTime.UtcNow;

        await _dbContext.SysUserNotices
            .Where(un => un.UserId == userId && un.IsRead == 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsRead, 1)
                .SetProperty(x => x.ReadTime, now)
                .SetProperty(x => x.UpdateTime, now), cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// 我的公告分页
    /// </summary>
    public async Task<PageResult<NoticePageVo>> GetMyNoticePageAsync(NoticePageQuery query, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var baseQuery =
            from un in _dbContext.SysUserNotices.AsNoTracking()
            join n in _dbContext.SysNotices.AsNoTracking() on un.NoticeId equals n.Id
            where un.UserId == userId
                && !un.IsDeleted
                && !n.IsDeleted
                && n.PublishStatus == PublishStatusPublished
            select new
            {
                n.Id,
                n.Title,
                n.Content,
                n.Type,
                n.Level,
                n.PublishStatus,
                n.PublishTime,
                un.IsRead,
            };

        if (query.IsRead.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IsRead == query.IsRead.Value);
        }

        baseQuery = baseQuery.OrderByDescending(x => x.PublishTime).ThenByDescending(x => x.Id);

        var total = await baseQuery.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;
        var rows = await baseQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows
            .Select(r => new NoticePageVo
            {
                Id = r.Id,
                Title = r.Title ?? string.Empty,
                Content = r.Content,
                Type = r.Type,
                Level = r.Level,
                PublishStatus = r.PublishStatus,
                IsRead = r.IsRead,
                PublishTime = r.PublishTime.HasValue ? r.PublishTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
            })
            .ToArray();

        return PageResult<NoticePageVo>.Success(list, total, pageNum, pageSize);
    }

    private void ValidateTargets(NoticeForm formData)
    {
        var targetType = formData.TargetType ?? TargetAll;
        if (targetType == TargetSpecified)
        {
            if (formData.TargetUserIds is null || formData.TargetUserIds.Count == 0)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "推送指定用户不能为空");
            }
        }
    }

    private long GetRequiredCurrentUserId()
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            throw new BusinessException(ResultCode.AccessTokenInvalid);
        }

        return userId.Value;
    }

    private static HashSet<long> ParseIdList(string? input)
    {
        var set = new HashSet<long>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return set;
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (long.TryParse(p, out var v) && v > 0)
            {
                set.Add(v);
            }
        }

        return set;
    }

    private static HashSet<long> ParseStringIdList(string? input)
    {
        var set = new HashSet<long>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return set;
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (long.TryParse(p, out var v) && v > 0)
            {
                set.Add(v);
            }
        }

        return set;
    }

    private static string? JoinIds(IReadOnlyCollection<string>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return null;
        }

        var list = ids
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return list.Length == 0 ? null : string.Join(',', list);
    }

    private static IReadOnlyCollection<string>? SplitIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return Array.Empty<string>();
        }

        return ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
