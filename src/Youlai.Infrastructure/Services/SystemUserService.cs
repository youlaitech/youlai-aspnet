using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.System.Dtos.User;
using Youlai.Application.System.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 用户服务
/// </summary>
internal sealed class SystemUserService : ISystemUserService
{
    private const string DefaultPassword = "123456";
    private static readonly TimeSpan VerifyCodeTtl = TimeSpan.FromMinutes(5);

    private readonly YoulaiDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly JwtTokenManager _tokenManager;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SystemUserService> _logger;

    public SystemUserService(
        YoulaiDbContext dbContext,
        ICurrentUser currentUser,
        IRolePermissionService rolePermissionService,
        IDataPermissionService dataPermissionService,
        JwtTokenManager tokenManager,
        IConnectionMultiplexer redis,
        ILogger<SystemUserService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _rolePermissionService = rolePermissionService;
        _dataPermissionService = dataPermissionService;
        _tokenManager = tokenManager;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// 当前登录用户信息
    /// </summary>
    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            return new CurrentUserDto();
        }

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId.Value && !u.IsDeleted)
            .Select(u => new { u.Id, u.Username, u.Nickname, u.Avatar })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return new CurrentUserDto();
        }

        var roles = _currentUser.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var perms = roles.Length == 0
            ? Array.Empty<string>()
            : (await _rolePermissionService.GetRolePermsAsync(roles, cancellationToken).ConfigureAwait(false))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        if (_currentUser.IsRoot && perms.Length == 0)
        {
            perms = new[] { "*:*:*" };
        }

        return new CurrentUserDto
        {
            UserId = user.Id,
            Username = user.Username ?? string.Empty,
            Nickname = user.Nickname ?? string.Empty,
            Avatar = user.Avatar,
            Roles = roles,
            Perms = perms,
        };
    }

    public async Task<PageResult<UserPageVo>> GetUserPageAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var users = _dbContext.SysUsers.AsNoTracking().Where(u => !u.IsDeleted);

        // 排除超级管理员，避免列表误操作
        var rootUserIds =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where !r.IsDeleted && r.Code == SecurityConstants.RootRoleCode
            select ur.UserId;

        users = users.Where(u => !rootUserIds.Contains(u.Id));

        // 数据权限过滤，确保用户只能看到授权范围内的数据
        users = _dataPermissionService.Apply(users, u => u.DeptId ?? 0, u => u.Id);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            users = users.Where(u => (u.Username != null && u.Username.Contains(keywords))
                || (u.Nickname != null && u.Nickname.Contains(keywords))
                || (u.Mobile != null && u.Mobile.Contains(keywords)));
        }

        if (query.Status.HasValue)
        {
            users = users.Where(u => u.Status == query.Status.Value);
        }

        if (query.DeptId.HasValue)
        {
            users = users.Where(u => u.DeptId == query.DeptId.Value);
        }

        var roleIdList = ParseLongList(query.RoleIds);
        if (roleIdList.Count > 0)
        {
            var roleUserIds = _dbContext.SysUserRoles
                .AsNoTracking()
                .Where(ur => roleIdList.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            users = users.Where(u => roleUserIds.Contains(u.Id));
        }

        var (startTime, endTime) = ParseTimeRange(query.CreateTime);
        if (startTime.HasValue)
        {
            users = users.Where(u => u.CreateTime != null && u.CreateTime.Value >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            users = users.Where(u => u.CreateTime != null && u.CreateTime.Value <= endTime.Value);
        }

        users = ApplySorting(users, query.Field, query.Direction);

        var total = await users.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<UserPageVo>.Success(Array.Empty<UserPageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;

        var pageRowsQuery =
            from u in users
            join d in _dbContext.SysDepts.AsNoTracking() on u.DeptId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.Username,
                u.Nickname,
                u.Mobile,
                u.Gender,
                u.Avatar,
                u.Email,
                u.Status,
                DeptName = d != null ? d.Name : null,
                u.CreateTime,
            };

        var pageRows = await pageRowsQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // 角色名称需要单独聚合，避免在主查询里引入过多 join
        var userIds = pageRows.Select(x => x.Id).ToArray();
        var roleNamesMap = await GetRoleNamesMapAsync(userIds, cancellationToken).ConfigureAwait(false);

        var list = pageRows
            .Select(x => new UserPageVo
            {
                Id = x.Id,
                Username = x.Username ?? string.Empty,
                Nickname = x.Nickname ?? string.Empty,
                Mobile = x.Mobile,
                Gender = x.Gender,
                Avatar = x.Avatar,
                Email = x.Email,
                Status = x.Status,
                DeptName = x.DeptName,
                RoleNames = roleNamesMap.TryGetValue(x.Id, out var rn) ? rn : null,
                CreateTime = x.CreateTime.HasValue ? x.CreateTime.Value.ToString("yyyy/MM/dd HH:mm") : null,
            })
            .ToArray();

        return PageResult<UserPageVo>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 用户表单
    /// </summary>
    public async Task<UserForm> GetUserFormAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Nickname,
                u.Gender,
                u.Mobile,
                u.Email,
                u.Avatar,
                u.DeptId,
                u.Status,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var roleIds = await _dbContext.SysUserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UserForm
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Gender = user.Gender,
            Mobile = user.Mobile,
            Email = user.Email,
            Avatar = user.Avatar,
            DeptId = user.DeptId,
            Status = user.Status,
            RoleIds = roleIds,
        };
    }

    /// <summary>
    /// 新增用户
    /// </summary>
    public async Task<bool> CreateUserAsync(UserForm formData, CancellationToken cancellationToken = default)
    {
        var username = formData.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名不能为空");
        }

        var exists = await _dbContext.SysUsers
            .AsNoTracking()
            .AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名已存在");
        }

        var now = DateTime.UtcNow;
        var createBy = _currentUser.UserId;

        var user = new SysUser
        {
            Username = username,
            Nickname = formData.Nickname?.Trim(),
            Gender = formData.Gender,
            Mobile = formData.Mobile?.Trim(),
            Email = formData.Email?.Trim(),
            Avatar = formData.Avatar?.Trim(),
            DeptId = formData.DeptId,
            Status = formData.Status ?? 1,
            Password = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
            CreateBy = createBy,
            CreateTime = now,
            UpdateBy = createBy,
            UpdateTime = now,
            IsDeleted = false,
        };

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        _dbContext.SysUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SaveUserRolesAsync(user.Id, formData.RoleIds, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<bool> UpdateUserAsync(long userId, UserForm formData, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.SysUsers
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var username = formData.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名不能为空");
        }

        var exists = await _dbContext.SysUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id != userId && u.Username == username && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名已存在");
        }

        user.Username = username;
        user.Nickname = formData.Nickname?.Trim();
        user.Gender = formData.Gender;
        user.Mobile = formData.Mobile?.Trim();
        user.Email = formData.Email?.Trim();
        user.Avatar = formData.Avatar?.Trim();
        user.DeptId = formData.DeptId;
        user.Status = formData.Status ?? user.Status;
        user.UpdateBy = _currentUser.UserId;
        user.UpdateTime = DateTime.UtcNow;

        // 角色关系更新与用户更新放在同一事务内
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SaveUserRolesAsync(userId, formData.RoleIds, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    public async Task<bool> DeleteUsersAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = ParseIdList(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的用户数据为空");
        }

        var users = await _dbContext.SysUsers
            .Where(u => idList.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (users.Count == 0)
        {
            return true;
        }

        // 软删除并统一更新时间
        var now = DateTime.UtcNow;
        var updateBy = _currentUser.UserId;
        foreach (var u in users)
        {
            u.IsDeleted = true;
            u.UpdateBy = updateBy;
            u.UpdateTime = now;
        }

        // 清理用户角色关联
        var roles = await _dbContext.SysUserRoles
            .Where(ur => idList.Contains(ur.UserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _dbContext.SysUserRoles.RemoveRange(roles);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 修改用户状态
    /// </summary>
    public async Task<bool> UpdateUserStatusAsync(long userId, int status, CancellationToken cancellationToken = default)
    {
        if (status is not (0 or 1))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户状态不正确");
        }

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Status, status)
                .SetProperty(u => u.UpdateBy, _currentUser.UserId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        return ok > 0;
    }

    public async Task<bool> UnbindMobileAsync(PasswordVerifyForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Password, u.Mobile })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        if (string.IsNullOrWhiteSpace(user.Mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前账号未绑定手机号");
        }

        if (string.IsNullOrWhiteSpace(formData.Password) || string.IsNullOrWhiteSpace(user.Password)
            || !BCrypt.Net.BCrypt.Verify(formData.Password, user.Password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前密码错误");
        }

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Mobile, (string?)null)
                .SetProperty(u => u.UpdateBy, userId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        return ok > 0;
    }

    public async Task<bool> UnbindEmailAsync(PasswordVerifyForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Password, u.Email })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前账号未绑定邮箱");
        }

        if (string.IsNullOrWhiteSpace(formData.Password) || string.IsNullOrWhiteSpace(user.Password)
            || !BCrypt.Net.BCrypt.Verify(formData.Password, user.Password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前密码错误");
        }

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Email, (string?)null)
                .SetProperty(u => u.UpdateBy, userId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    public async Task<bool> ResetUserPasswordAsync(long userId, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "密码不能为空");
        }

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Password, BCrypt.Net.BCrypt.HashPassword(password))
                .SetProperty(u => u.UpdateBy, _currentUser.UserId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        if (ok > 0)
        {
            await _tokenManager.InvalidateUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        return ok > 0;
    }

    /// <summary>
    /// 下载导入模板
    /// </summary>
    public Task<IReadOnlyCollection<byte>> DownloadUserImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        var csv = "username,nickname,mobile,email,gender,status\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return Task.FromResult<IReadOnlyCollection<byte>>(bytes);
    }

    /// <summary>
    /// 导出用户
    /// </summary>
    public async Task<IReadOnlyCollection<byte>> ExportUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.SysUsers.AsNoTracking().Where(u => !u.IsDeleted);

        // 排除超级管理员，避免导出误操作
        var rootUserIds =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where !r.IsDeleted && r.Code == SecurityConstants.RootRoleCode
            select ur.UserId;

        users = users.Where(u => !rootUserIds.Contains(u.Id));
        // 数据权限过滤，和列表保持一致
        users = _dataPermissionService.Apply(users, u => u.DeptId ?? 0, u => u.Id);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            users = users.Where(u => (u.Username != null && u.Username.Contains(keywords))
                || (u.Nickname != null && u.Nickname.Contains(keywords))
                || (u.Mobile != null && u.Mobile.Contains(keywords)));
        }

        if (query.Status.HasValue)
        {
            users = users.Where(u => u.Status == query.Status.Value);
        }

        if (query.DeptId.HasValue)
        {
            users = users.Where(u => u.DeptId == query.DeptId.Value);
        }

        var roleIdList = ParseLongList(query.RoleIds);
        if (roleIdList.Count > 0)
        {
            var roleUserIds = _dbContext.SysUserRoles
                .AsNoTracking()
                .Where(ur => roleIdList.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            users = users.Where(u => roleUserIds.Contains(u.Id));
        }

        var (startTime, endTime) = ParseTimeRange(query.CreateTime);
        if (startTime.HasValue)
        {
            users = users.Where(u => u.CreateTime != null && u.CreateTime.Value >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            users = users.Where(u => u.CreateTime != null && u.CreateTime.Value <= endTime.Value);
        }

        users = ApplySorting(users, query.Field, query.Direction);

        var rowsQuery =
            from u in users
            join d in _dbContext.SysDepts.AsNoTracking() on u.DeptId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.Username,
                u.Nickname,
                u.Mobile,
                u.Email,
                u.Gender,
                u.Status,
                DeptName = d != null ? d.Name : null,
                u.CreateTime,
            };

        var rows = await rowsQuery
            // 导出行数做上限保护
            .Take(5000)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // 角色名称单独聚合，避免查询过重
        var userIds = rows.Select(r => r.Id).ToArray();
        var roleNamesMap = await GetRoleNamesMapAsync(userIds, cancellationToken).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("id,username,nickname,mobile,email,gender,status,deptName,roleNames,createTime");
        foreach (var r in rows)
        {
            sb
                .Append(r.Id).Append(',')
                .Append(EscapeCsv(r.Username)).Append(',')
                .Append(EscapeCsv(r.Nickname)).Append(',')
                .Append(EscapeCsv(r.Mobile)).Append(',')
                .Append(EscapeCsv(r.Email)).Append(',')
                .Append(r.Gender?.ToString() ?? string.Empty).Append(',')
                .Append(r.Status).Append(',')
                .Append(EscapeCsv(r.DeptName)).Append(',')
                .Append(EscapeCsv(roleNamesMap.TryGetValue(r.Id, out var rn) ? rn : null)).Append(',')
                .Append(r.CreateTime.HasValue ? r.CreateTime.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty)
                .AppendLine();
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
        return bytes;
    }

    /// <summary>
    /// 导入用户
    /// </summary>
    public async Task<ExcelResult> ImportUsersAsync(long deptId, Stream content, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        _ = db;

        // CSV 按行读取，逐条校验并累积错误信息
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var messages = new List<string>();
        var valid = 0;
        var invalid = 0;

        var header = await reader.ReadLineAsync().ConfigureAwait(false);
        if (header is null)
        {
            return new ExcelResult
            {
                Code = ResultCode.Success.Code(),
                ValidCount = 0,
                InvalidCount = 0,
                MessageList = Array.Empty<string>(),
            };
        }

        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.None);
            var username = parts.ElementAtOrDefault(0)?.Trim();
            var nickname = parts.ElementAtOrDefault(1)?.Trim();
            var mobile = parts.ElementAtOrDefault(2)?.Trim();
            var email = parts.ElementAtOrDefault(3)?.Trim();
            var gender = TryParseInt(parts.ElementAtOrDefault(4));
            var status = TryParseInt(parts.ElementAtOrDefault(5)) ?? 1;

            // 账号名必填，且不能重复
            if (string.IsNullOrWhiteSpace(username))
            {
                invalid++;
                messages.Add("用户名不能为空");
                continue;
            }

            var exists = await _dbContext.SysUsers
                .AsNoTracking()
                .AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                invalid++;
                messages.Add($"用户名已存在: {username}");
                continue;
            }

            var now = DateTime.UtcNow;
            var createBy = _currentUser.UserId;

            var user = new SysUser
            {
                Username = username,
                Nickname = nickname,
                Mobile = mobile,
                Email = email,
                Gender = gender,
                Status = status is 0 or 1 ? status : 1,
                DeptId = deptId,
                Password = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                CreateBy = createBy,
                CreateTime = now,
                UpdateBy = createBy,
                UpdateTime = now,
                IsDeleted = false,
            };

            _dbContext.SysUsers.Add(user);
            valid++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ExcelResult
        {
            Code = ResultCode.Success.Code(),
            ValidCount = valid,
            InvalidCount = invalid,
            MessageList = messages,
        };
    }

    /// <summary>
    /// 个人资料
    /// </summary>
    public async Task<UserProfileVo> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var rowQuery =
            from u in _dbContext.SysUsers.AsNoTracking()
            join d in _dbContext.SysDepts.AsNoTracking() on u.DeptId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            where u.Id == userId && !u.IsDeleted
            select new
            {
                u.Id,
                u.Username,
                u.Nickname,
                u.Avatar,
                u.Gender,
                u.Mobile,
                u.Email,
                DeptName = d != null ? d.Name : null,
                u.CreateTime,
            };

        var row = await rowQuery.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var roleNamesMap = await GetRoleNamesMapAsync(new[] { userId }, cancellationToken).ConfigureAwait(false);

        return new UserProfileVo
        {
            Id = row.Id,
            Username = row.Username,
            Nickname = row.Nickname,
            Avatar = row.Avatar,
            Gender = row.Gender,
            Mobile = row.Mobile,
            Email = row.Email,
            DeptName = row.DeptName,
            RoleNames = roleNamesMap.TryGetValue(userId, out var rn) ? rn : null,
            CreateTime = row.CreateTime.HasValue ? row.CreateTime.Value.ToString("yyyy/MM/dd HH:mm") : null,
        };
    }

    /// <summary>
    /// 更新个人资料
    /// </summary>
    public async Task<bool> UpdateProfileAsync(UserProfileForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var user = await _dbContext.SysUsers
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var updated = false;

        if (formData.Nickname is not null)
        {
            user.Nickname = formData.Nickname.Trim();
            updated = true;
        }

        if (formData.Avatar is not null)
        {
            user.Avatar = formData.Avatar.Trim();
            updated = true;
        }

        if (formData.Gender.HasValue)
        {
            user.Gender = formData.Gender;
            updated = true;
        }

        if (!updated)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "请至少修改一项");
        }

        user.UpdateBy = userId;
        user.UpdateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(PasswordChangeForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();

        var user = await _dbContext.SysUsers
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var oldPwd = formData.OldPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(user.Password) || !BCrypt.Net.BCrypt.Verify(oldPwd, user.Password))
        {
            throw new BusinessException(ResultCode.UserPasswordError, "原密码错误");
        }

        var newPwd = formData.NewPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newPwd))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码不能为空");
        }

        if (BCrypt.Net.BCrypt.Verify(newPwd, user.Password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码不能与原密码相同");
        }

        if (!string.Equals(formData.NewPassword, formData.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码和确认密码不一致");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPwd);
        user.UpdateBy = userId;
        user.UpdateTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _tokenManager.InvalidateUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 发送手机验证码
    /// </summary>
    public async Task<bool> SendMobileCodeAsync(string mobile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号不能为空");
        }

        var code = "1234";
        _logger.LogInformation("[SendMobileCode] mobile={Mobile} code={Code}", mobile, code);

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        await db.StringSetAsync(key, code, VerifyCodeTtl).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 绑定或修改手机号
    /// </summary>
    public async Task<bool> BindOrChangeMobileAsync(MobileUpdateForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();
        var mobile = formData.Mobile?.Trim();
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号不能为空");
        }

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Password })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        if (string.IsNullOrWhiteSpace(formData.Password) || string.IsNullOrWhiteSpace(user.Password)
            || !BCrypt.Net.BCrypt.Verify(formData.Password, user.Password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前密码错误");
        }

        var mobileExists = await _dbContext.SysUsers
            .AsNoTracking()
            .AnyAsync(u => !u.IsDeleted && u.Id != userId && u.Mobile == mobile, cancellationToken)
            .ConfigureAwait(false);

        if (mobileExists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号已被其他账号绑定");
        }

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile);
        var cached = await db.StringGetAsync(key).ConfigureAwait(false);

        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached.ToString(), formData.Code, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await db.KeyDeleteAsync(key).ConfigureAwait(false);

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Mobile, mobile)
                .SetProperty(u => u.UpdateBy, userId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    public async Task<bool> SendEmailCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "邮箱不能为空");
        }

        var code = "1234";
        _logger.LogInformation("[SendEmailCode] email={Email} code={Code}", email, code);

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.EmailCode, email.Trim());
        await db.StringSetAsync(key, code, VerifyCodeTtl).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 绑定或修改邮箱
    /// </summary>
    public async Task<bool> BindOrChangeEmailAsync(EmailUpdateForm formData, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredCurrentUserId();
        var email = formData.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "邮箱不能为空");
        }

        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Password })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        if (string.IsNullOrWhiteSpace(formData.Password) || string.IsNullOrWhiteSpace(user.Password)
            || !BCrypt.Net.BCrypt.Verify(formData.Password, user.Password))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前密码错误");
        }

        var emailExists = await _dbContext.SysUsers
            .AsNoTracking()
            .AnyAsync(u => !u.IsDeleted && u.Id != userId && u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (emailExists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "邮箱已被其他账号绑定");
        }

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeyConstants.Captcha.EmailCode, email);
        var cached = await db.StringGetAsync(key).ConfigureAwait(false);

        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached.ToString(), formData.Code, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await db.KeyDeleteAsync(key).ConfigureAwait(false);

        var ok = await _dbContext.SysUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Email, email)
                .SetProperty(u => u.UpdateBy, userId)
                .SetProperty(u => u.UpdateTime, DateTime.UtcNow), cancellationToken)
            .ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 用户下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<long>>> GetUserOptionsAsync(CancellationToken cancellationToken = default)
    {
        var rootUserIds =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where !r.IsDeleted && r.Code == SecurityConstants.RootRoleCode
            select ur.UserId;

        var rows = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Status == 1 && !rootUserIds.Contains(u.Id))
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.Nickname, u.Username })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(u => new Option<long>(u.Id, string.IsNullOrWhiteSpace(u.Nickname) ? (u.Username ?? u.Id.ToString()) : u.Nickname!))
            .ToArray();
    }

    private IQueryable<SysUser> ApplySorting(
        IQueryable<SysUser> query,
        string? field,
        string? direction)
    {
        var desc = !string.Equals(direction, "ASC", StringComparison.OrdinalIgnoreCase);
        var f = field?.Trim();

        return f switch
        {
            "update_time" => desc
                ? query.OrderByDescending(u => u.UpdateTime)
                : query.OrderBy(u => u.UpdateTime),
            "create_time" or null or "" => desc
                ? query.OrderByDescending(u => u.CreateTime)
                : query.OrderBy(u => u.CreateTime),
            _ => desc
                ? query.OrderByDescending(u => u.CreateTime)
                : query.OrderBy(u => u.CreateTime),
        };
    }

    private async Task<Dictionary<long, string>> GetRoleNamesMapAsync(long[] userIds, CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return new Dictionary<long, string>();
        }

        var query =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
                && !r.IsDeleted
                && r.Status == 1
            select new { ur.UserId, r.Name };

        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(",", g.Select(x => x.Name).Distinct(StringComparer.Ordinal))
            );
    }

    private static HashSet<long> ParseLongList(string? input)
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

    private static (DateTime? Start, DateTime? End) ParseTimeRange(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return (null, null);
        }

        return (TryParseDateTime(parts[0]), TryParseDateTime(parts[1]));
    }

    private static DateTime? TryParseDateTime(string input)
    {
        return DateTime.TryParse(input, out var dt) ? dt : null;
    }

    private static int? TryParseInt(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        return int.TryParse(input.Trim(), out var v) ? v : null;
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

    private async Task SaveUserRolesAsync(long userId, IReadOnlyCollection<long>? roleIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SysUserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.Count > 0)
        {
            _dbContext.SysUserRoles.RemoveRange(existing);
        }

        if (roleIds is null || roleIds.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var distinct = roleIds.Where(r => r > 0).Distinct().ToArray();
        if (distinct.Length == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        foreach (var roleId in distinct)
        {
            _dbContext.SysUserRoles.Add(new SysUserRole { UserId = userId, RoleId = roleId });
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private static string EscapeCsv(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        if (!input.Contains('"') && !input.Contains(',') && !input.Contains('\n') && !input.Contains('\r'))
        {
            return input;
        }

        return "\"" + input.Replace("\"", "\"\"") + "\"";
    }
}
