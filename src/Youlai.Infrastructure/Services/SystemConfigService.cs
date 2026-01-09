using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Data;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 参数配置服务
/// </summary>
/// <remarks>
/// 提供参数配置的查询与维护，并支持缓存刷新
/// </remarks>
internal sealed class SystemConfigService : ISystemConfigService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IConnectionMultiplexer _redis;

    public SystemConfigService(AppDbContext dbContext, ICurrentUser currentUser, IConnectionMultiplexer redis)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _redis = redis;
    }

    /// <summary>
    /// 配置分页
    /// </summary>
    public async Task<PageResult<ConfigPageVo>> GetConfigPageAsync(ConfigPageQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var q = _dbContext.SysConfigs
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            q = q.Where(x => x.ConfigKey.Contains(keywords) || x.ConfigName.Contains(keywords));
        }

        q = q.OrderByDescending(x => x.Id);

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<ConfigPageVo>.Success(Array.Empty<ConfigPageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;
        var list = await q
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new ConfigPageVo
            {
                Id = x.Id,
                ConfigName = x.ConfigName,
                ConfigKey = x.ConfigKey,
                ConfigValue = x.ConfigValue,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PageResult<ConfigPageVo>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 配置表单
    /// </summary>
    public async Task<ConfigForm> GetConfigFormAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置不存在");
        }

        return new ConfigForm
        {
            Id = entity.Id,
            ConfigName = entity.ConfigName,
            ConfigKey = entity.ConfigKey,
            ConfigValue = entity.ConfigValue,
            Remark = entity.Remark,
        };
    }

    /// <summary>
    /// 新增配置
    /// </summary>
    public async Task<bool> SaveConfigAsync(ConfigForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.ConfigName))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(form.ConfigKey))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置键不能为空");
        }

        if (string.IsNullOrWhiteSpace(form.ConfigValue))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置值不能为空");
        }

        var configName = form.ConfigName.Trim();
        var configKey = form.ConfigKey.Trim();

        var exists = await _dbContext.SysConfigs
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.ConfigKey == configKey, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置键已存在");
        }

        var entity = new SysConfig
        {
            ConfigName = configName,
            ConfigKey = configKey,
            ConfigValue = form.ConfigValue.Trim(),
            Remark = form.Remark,
            CreateBy = _currentUser.UserId,
            CreateTime = DateTime.Now,
            IsDeleted = false,
        };

        _dbContext.SysConfigs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public async Task<bool> UpdateConfigAsync(long id, ConfigForm form, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysConfigs
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置不存在");
        }

        if (string.IsNullOrWhiteSpace(form.ConfigName))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(form.ConfigKey))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置键不能为空");
        }

        if (string.IsNullOrWhiteSpace(form.ConfigValue))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置值不能为空");
        }

        var configName = form.ConfigName.Trim();
        var configKey = form.ConfigKey.Trim();

        var exists = await _dbContext.SysConfigs
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id != id && x.ConfigKey == configKey, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "配置键已存在");
        }

        entity.ConfigName = configName;
        entity.ConfigKey = configKey;
        entity.ConfigValue = form.ConfigValue.Trim();
        entity.Remark = form.Remark;
        entity.UpdateBy = _currentUser.UserId;
        entity.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    public async Task<bool> DeleteConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysConfigs
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return true;
        }

        entity.IsDeleted = true;
        entity.UpdateBy = _currentUser.UserId;
        entity.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 刷新配置缓存
    /// </summary>
    public async Task<bool> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.SysConfigs
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeyConstants.System.Config).ConfigureAwait(false);

        if (list.Count == 0)
        {
            return true;
        }

        var entries = list
            .Where(x => !string.IsNullOrWhiteSpace(x.ConfigKey))
            .Select(x => new HashEntry(x.ConfigKey, x.ConfigValue ?? string.Empty))
            .ToArray();

        if (entries.Length > 0)
        {
            await db.HashSetAsync(RedisKeyConstants.System.Config, entries).ConfigureAwait(false);
        }

        return true;
    }
}
