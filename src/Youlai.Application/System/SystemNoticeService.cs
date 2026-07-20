using Microsoft.Extensions.Logging;
using Youlai.Application.Common;
using Youlai.Application.Exceptions;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.System;
using Youlai.Application.System.Models.Notice;
using Youlai.Domain.Entities;

using Youlai.Application.Extensions;

namespace Youlai.Application.System;

/// <summary>
/// 公告服务（分页、CRUD、发布/撤回）
/// </summary>
internal sealed class SystemNoticeService : ISystemNoticeService
{
    private const int PublishStatusDraft = 0;
    private const int PublishStatusPublished = 1;
    private const int PublishStatusRevoked = -1;

    private const int TargetAll = 1;
    private const int TargetSpecified = 2;

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<SysNotice> _noticeRepo;
    private readonly IRepository<SysUserNotice> _userNoticeRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISseService _sseService;
    private readonly ILogger<SystemNoticeService> _logger;

    public SystemNoticeService(
        IReadOnlyDbContext readDb,
        IRepository<SysNotice> noticeRepo,
        IRepository<SysUserNotice> userNoticeRepo,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ISseService sseService,
        ILogger<SystemNoticeService> logger)
    {
        _readDb = readDb;
        _noticeRepo = noticeRepo;
        _userNoticeRepo = userNoticeRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _sseService = sseService;
        _logger = logger;
    }

    /// <summary>
    /// 公告分页
    /// </summary>
    public async Task<PageResult<NoticePageVo>> GetNoticePageAsync(NoticeQuery query, CancellationToken cancellationToken = default)
    {
        var (pageNum, pageSize) = query.Normalize();

        var notices = _readDb.Query<SysNotice>().Where(n => !n.IsDeleted);

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

        var total = await _readDb.CountAsync(notices, cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize);
        }

        var rows = await _readDb.ToListAsync(
            (from n in notices
             join u in _readDb.Query<SysUser>() on n.PublisherId equals u.Id into pubJoin
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
             })
            .Skip((pageNum - 1) * pageSize)
            .Take(pageSize),
            cancellationToken).ConfigureAwait(false);

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
        var notice = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysNotice>().Where(n => n.Id == id && !n.IsDeleted),
            cancellationToken).ConfigureAwait(false);

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
        var uid = _currentUser.GetRequiredUserId();

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

        _noticeRepo.Add(notice);
        return await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 更新公告
    /// </summary>
    public async Task<bool> UpdateNoticeAsync(long id, NoticeForm formData, CancellationToken cancellationToken = default)
    {
        ValidateTargets(formData);

        var notice = await _noticeRepo.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (notice is null || notice.IsDeleted)
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
        notice.UpdateBy = _currentUser.GetRequiredUserId();
        notice.UpdateTime = DateTime.UtcNow;

        _noticeRepo.Update(notice);
        return await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 批量删除公告
    /// </summary>
    public async Task<bool> DeleteNoticesAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = IdCollectionExtensions.ParsePositiveLongIds(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的通知公告数据为空");
        }

        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _noticeRepo.BatchUpdateAsync(
            n => idList.Contains(n.Id) && !n.IsDeleted,
            n =>
            {
                n.IsDeleted = true;
                n.UpdateBy = _currentUser.GetRequiredUserId();
                n.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

        await _userNoticeRepo.BatchUpdateAsync(
            un => idList.Contains(un.NoticeId) && !un.IsDeleted,
            un => un.IsDeleted = true,
            cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 发布公告
    /// </summary>
    public async Task<bool> PublishNoticeAsync(long id, CancellationToken cancellationToken = default)
    {
        var notice = await _noticeRepo.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (notice is null || notice.IsDeleted)
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
        var uid = _currentUser.GetRequiredUserId();

        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        notice.PublishStatus = PublishStatusPublished;
        notice.PublisherId = uid;
        notice.PublishTime = now;
        notice.RevokeTime = null;
        notice.UpdateBy = uid;
        notice.UpdateTime = now;

        _noticeRepo.Update(notice);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _userNoticeRepo.DeleteAsync(un => un.NoticeId == id, cancellationToken).ConfigureAwait(false);

        var targetUserIds = notice.TargetType == TargetSpecified
            ? IdCollectionExtensions.ParsePositiveLongIds(notice.TargetUserIds)
            : new HashSet<long>();

        var usersQuery = _readDb.Query<SysUser>().Where(u => !u.IsDeleted && u.Status == 1);
        if (notice.TargetType == TargetSpecified)
        {
            usersQuery = usersQuery.Where(u => targetUserIds.Contains(u.Id));
        }

        var users = await _readDb.ToListAsync(usersQuery.Select(u => new { u.Id }), cancellationToken).ConfigureAwait(false);

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
            _userNoticeRepo.AddRange(userNotices);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        // SSE通知目标用户
        var noticeData = new
        {
            id,
            title = notice.Title,
            type = notice.Type,
            level = notice.Level,
            publishStatus = 1,
            publishTime = notice.PublishTime?.ToString("yyyy-MM-dd HH:mm"),
        };

        var sseTargetIds = await ResolveSseTargetIdsAsync(notice.TargetType, notice.TargetUserIds, cancellationToken).ConfigureAwait(false);
        foreach (var userIdStr in sseTargetIds)
        {
            _logger.LogInformation("[Notice] Sending SSE to userId: {UserId}, event: notice", userIdStr);
            await _sseService.SendToUserAsync(userIdStr, "notice", noticeData, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 撤回公告
    /// </summary>
    public async Task<bool> RevokeNoticeAsync(long id, CancellationToken cancellationToken = default)
    {
        var notice = await _noticeRepo.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (notice is null || notice.IsDeleted)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        if (notice.PublishStatus != PublishStatusPublished)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告未发布或已撤回");
        }

        var now = DateTime.UtcNow;
        var uid = _currentUser.GetRequiredUserId();

        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        notice.PublishStatus = PublishStatusRevoked;
        notice.RevokeTime = now;
        notice.UpdateBy = uid;
        notice.UpdateTime = now;

        _noticeRepo.Update(notice);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _userNoticeRepo.DeleteAsync(un => un.NoticeId == id, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        // SSE通知目标用户移除该通知
        var sseTargetIds = await ResolveSseTargetIdsAsync(notice.TargetType, notice.TargetUserIds, cancellationToken).ConfigureAwait(false);
        foreach (var userIdStr in sseTargetIds)
        {
            await _sseService.SendToUserAsync(userIdStr, "notice-revoke", new { id }, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 公告详情
    /// </summary>
    public async Task<NoticeDetailVo> GetNoticeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        var noticeQuery =
            from n in _readDb.Query<SysNotice>()
            join u in _readDb.Query<SysUser>() on n.PublisherId equals u.Id into pubJoin
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

        var row = await _readDb.FirstOrDefaultAsync(noticeQuery, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "通知公告不存在");
        }

        var userId = _currentUser.UserId;
        if (userId.HasValue && userId.Value > 0)
        {
            await _userNoticeRepo.BatchUpdateAsync(
                un => un.NoticeId == id && un.UserId == userId.Value && un.IsRead == 0,
                un =>
                {
                    un.IsRead = 1;
                    un.ReadTime = DateTime.UtcNow;
                    un.UpdateTime = DateTime.UtcNow;
                },
                cancellationToken).ConfigureAwait(false);
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
        var userId = _currentUser.GetRequiredUserId();
        var now = DateTime.UtcNow;

        await _userNoticeRepo.BatchUpdateAsync(
            un => un.UserId == userId && un.IsRead == 0,
            un =>
            {
                un.IsRead = 1;
                un.ReadTime = now;
                un.UpdateTime = now;
            },
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// 我的公告分页
    /// </summary>
    public async Task<PageResult<NoticePageVo>> GetMyNoticePageAsync(NoticeQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();
        var (pageNum, pageSize) = query.Normalize();

        var baseQuery =
            from un in _readDb.Query<SysUserNotice>()
            join n in _readDb.Query<SysNotice>() on un.NoticeId equals n.Id
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

        var total = await _readDb.CountAsync(baseQuery, cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize);
        }

        var rows = await _readDb.ToListAsync(
            baseQuery.Skip((pageNum - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// 指定用户推送时校验目标用户不为空
    /// </summary>
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

    /// <summary>
    /// 去重去空后拼成逗号分隔串，空集合返回 null
    /// </summary>
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

    /// <summary>
    /// 逗号分隔串拆为数组，空串返回空集合
    /// </summary>
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

    /// <summary>
    /// 解析 SSE 通知目标用户ID
    /// </summary>
    private async Task<List<string>> ResolveSseTargetIdsAsync(
        int targetType,
        string? targetUserIds,
        CancellationToken cancellationToken)
    {
        var allUsers = await _readDb.ToListAsync(
            _readDb.Query<SysUser>().Where(u => !u.IsDeleted && u.Status == 1).Select(u => u.Id),
            cancellationToken).ConfigureAwait(false);

        if (targetType == TargetSpecified)
        {
            var specifiedIds = IdCollectionExtensions.ParsePositiveLongIds(targetUserIds);
            return allUsers
                .Where(id => specifiedIds.Contains(id))
                .Select(id => id.ToString())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        // 推送全员
        return allUsers
            .Select(id => id.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
