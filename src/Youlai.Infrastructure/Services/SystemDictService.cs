using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Services;
using Youlai.Application.System.Dtos.Dict;
using Youlai.Application.System.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 字典服务
/// </summary>
internal sealed class SystemDictService : ISystemDictService
{
    private readonly YoulaiDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IWebSocketService _webSocketService;

    public SystemDictService(YoulaiDbContext dbContext, ICurrentUser currentUser, IWebSocketService webSocketService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _webSocketService = webSocketService;
    }

    /// <summary>
    /// 字典分页
    /// </summary>
    public async Task<PageResult<DictPageVo>> GetDictPageAsync(DictQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var dicts = _dbContext.SysDicts.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            dicts = dicts.Where(d => (d.Name != null && d.Name.Contains(keywords))
                || (d.DictCode != null && d.DictCode.Contains(keywords)));
        }

        if (query.Status.HasValue)
        {
            dicts = dicts.Where(d => d.Status == query.Status.Value);
        }

        dicts = dicts.OrderByDescending(d => d.Id);

        var total = await dicts.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<DictPageVo>.Success(Array.Empty<DictPageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;

        var rows = await dicts
            .Skip(skip)
            .Take(pageSize)
            .Select(d => new DictPageVo
            {
                Id = d.Id,
                Name = d.Name ?? string.Empty,
                DictCode = d.DictCode ?? string.Empty,
                Status = d.Status ?? 0,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PageResult<DictPageVo>.Success(rows, total, pageNum, pageSize);
    }

    /// <summary>
    /// 字典下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<string>>> GetDictListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.SysDicts
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == 1)
            .OrderBy(d => d.Id)
            .Select(d => new { d.DictCode, d.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Where(d => !string.IsNullOrWhiteSpace(d.DictCode))
            .Select(d => new Option<string>(d.DictCode!, d.Name ?? d.DictCode!))
            .ToArray();
    }

    /// <summary>
    /// 字典表单
    /// </summary>
    public async Task<DictForm> GetDictFormAsync(long id, CancellationToken cancellationToken = default)
    {
        var dict = await _dbContext.SysDicts
            .AsNoTracking()
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (dict is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典不存在");
        }

        return new DictForm
        {
            Id = dict.Id,
            Name = dict.Name,
            DictCode = dict.DictCode,
            Status = dict.Status,
            Remark = dict.Remark,
        };
    }

    /// <summary>
    /// 新增字典
    /// </summary>
    public async Task<bool> CreateDictAsync(DictForm formData, CancellationToken cancellationToken = default)
    {
        var dictCode = formData.DictCode?.Trim();
        if (string.IsNullOrWhiteSpace(dictCode))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典编码不能为空");
        }

        var exists = await _dbContext.SysDicts
            .AsNoTracking()
            .AnyAsync(d => !d.IsDeleted && d.DictCode == dictCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典编码已存在");
        }

        var now = DateTime.UtcNow;
        var uid = _currentUser.UserId;

        var dict = new SysDict
        {
            DictCode = dictCode,
            Name = formData.Name?.Trim(),
            Status = formData.Status ?? 1,
            Remark = formData.Remark,
            CreateBy = uid,
            CreateTime = now,
            UpdateBy = uid,
            UpdateTime = now,
            IsDeleted = false,
        };

        _dbContext.SysDicts.Add(dict);
        var saved = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        if (saved)
        {
            await _webSocketService.BroadcastDictChangeAsync(dictCode, cancellationToken).ConfigureAwait(false);
        }

        return saved;
    }

    /// <summary>
    /// 更新字典
    /// </summary>
    public async Task<bool> UpdateDictAsync(long id, DictForm formData, CancellationToken cancellationToken = default)
    {
        var dict = await _dbContext.SysDicts
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (dict is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典不存在");
        }

        var newDictCode = formData.DictCode?.Trim();
        if (string.IsNullOrWhiteSpace(newDictCode))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典编码不能为空");
        }

        var oldDictCode = dict.DictCode;

        if (!string.Equals(oldDictCode, newDictCode, StringComparison.Ordinal))
        {
            var exists = await _dbContext.SysDicts
                .AsNoTracking()
                .AnyAsync(d => !d.IsDeleted && d.DictCode == newDictCode, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "字典编码已存在");
            }
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        dict.DictCode = newDictCode;
        dict.Name = formData.Name?.Trim();
        dict.Status = formData.Status ?? dict.Status;
        dict.Remark = formData.Remark;
        dict.UpdateBy = _currentUser.UserId;
        dict.UpdateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(oldDictCode) && !string.Equals(oldDictCode, newDictCode, StringComparison.Ordinal))
        {
            await _dbContext.SysDictItems
                .Where(i => i.DictCode == oldDictCode)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.DictCode, newDictCode), cancellationToken)
                .ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        await _webSocketService.BroadcastDictChangeAsync(newDictCode, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(oldDictCode) && !string.Equals(oldDictCode, newDictCode, StringComparison.Ordinal))
        {
            await _webSocketService.BroadcastDictChangeAsync(oldDictCode, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 批量删除字典
    /// </summary>
    public async Task<bool> DeleteDictsAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = ParseIdList(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的字典数据为空");
        }

        var dictCodes = await _dbContext.SysDicts
            .AsNoTracking()
            .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
            .Select(d => d.DictCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SysDicts
            .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.UpdateBy, _currentUser.UserId)
                .SetProperty(x => x.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        var codeList = dictCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToArray();
        if (codeList.Length > 0)
        {
            await _dbContext.SysDictItems
                .Where(i => i.DictCode != null && codeList.Contains(i.DictCode))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var code in codeList)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var dictCode = code;
            await _webSocketService.BroadcastDictChangeAsync(dictCode!, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 字典项分页
    /// </summary>
    public async Task<PageResult<DictItemPageVo>> GetDictItemPageAsync(string dictCode, DictItemQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var items = _dbContext.SysDictItems.AsNoTracking().Where(i => i.DictCode == dictCode);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            items = items.Where(i => (i.Label != null && i.Label.Contains(keywords))
                || (i.Value != null && i.Value.Contains(keywords)));
        }

        items = items.OrderBy(i => i.Sort).ThenBy(i => i.Id);

        var total = await items.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<DictItemPageVo>.Success(Array.Empty<DictItemPageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;
        var rows = await items
            .Skip(skip)
            .Take(pageSize)
            .Select(i => new DictItemPageVo
            {
                Id = i.Id,
                DictCode = i.DictCode ?? string.Empty,
                Label = i.Label ?? string.Empty,
                Value = i.Value ?? string.Empty,
                Status = i.Status ?? 0,
                Sort = i.Sort,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PageResult<DictItemPageVo>.Success(rows, total, pageNum, pageSize);
    }

    /// <summary>
    /// 字典项列表
    /// </summary>
    public async Task<IReadOnlyCollection<DictItemOption>> GetDictItemsAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.SysDictItems
            .AsNoTracking()
            .Where(i => i.DictCode == dictCode && i.Status == 1)
            .OrderBy(i => i.Sort)
            .ThenBy(i => i.Id)
            .Select(i => new DictItemOption
            {
                Value = i.Value ?? string.Empty,
                Label = i.Label ?? string.Empty,
                TagType = i.TagType,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    /// <summary>
    /// 字典项表单
    /// </summary>
    public async Task<DictItemForm> GetDictItemFormAsync(string dictCode, long itemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.SysDictItems
            .AsNoTracking()
            .Where(i => i.Id == itemId && i.DictCode == dictCode)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典项不存在");
        }

        return new DictItemForm
        {
            Id = item.Id,
            DictCode = item.DictCode,
            Label = item.Label,
            Value = item.Value,
            Status = item.Status,
            Sort = item.Sort,
            TagType = item.TagType,
        };
    }

    /// <summary>
    /// 新增字典项
    /// </summary>
    public async Task<bool> CreateDictItemAsync(string dictCode, DictItemForm formData, CancellationToken cancellationToken = default)
    {
        var item = new SysDictItem
        {
            DictCode = dictCode,
            Label = formData.Label?.Trim(),
            Value = formData.Value?.Trim(),
            Status = formData.Status ?? 1,
            Sort = formData.Sort ?? 0,
            TagType = formData.TagType,
            CreateBy = _currentUser.UserId,
            CreateTime = DateTime.UtcNow,
            UpdateBy = _currentUser.UserId,
            UpdateTime = DateTime.UtcNow,
        };

        _dbContext.SysDictItems.Add(item);
        var ok = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        if (ok)
        {
            await _webSocketService.BroadcastDictChangeAsync(dictCode, cancellationToken).ConfigureAwait(false);
        }

        return ok;
    }

    /// <summary>
    /// 更新字典项
    /// </summary>
    public async Task<bool> UpdateDictItemAsync(string dictCode, long itemId, DictItemForm formData, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.SysDictItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DictCode == dictCode, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "字典项不存在");
        }

        item.Label = formData.Label?.Trim();
        item.Value = formData.Value?.Trim();
        item.Status = formData.Status ?? item.Status;
        item.Sort = formData.Sort ?? item.Sort;
        item.TagType = formData.TagType;
        item.UpdateBy = _currentUser.UserId;
        item.UpdateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _webSocketService.BroadcastDictChangeAsync(dictCode, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 批量删除字典项
    /// </summary>
    public async Task<bool> DeleteDictItemsAsync(string dictCode, string ids, CancellationToken cancellationToken = default)
    {
        var idList = ParseIdList(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的字典项数据为空");
        }

        await _dbContext.SysDictItems
            .Where(i => i.DictCode == dictCode && idList.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await _webSocketService.BroadcastDictChangeAsync(dictCode, cancellationToken).ConfigureAwait(false);
        return true;
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
}
