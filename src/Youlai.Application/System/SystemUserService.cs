using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Youlai.Application.Common;
using Youlai.Application.Sse;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Models;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.System.Models.User;
using Youlai.Domain.Entities;

using Youlai.Application.Extensions;

namespace Youlai.Application.System;

/// <summary>
/// 用户服务
/// </summary>
internal sealed class SystemUserService : ISystemUserService
{
    private static readonly TimeSpan VerifyCodeTtl = TimeSpan.FromMinutes(5);

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<SysUser> _userRepo;
    private readonly IRepository<SysUserRole> _userRoleRepo;
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly ITokenManager _tokenManager;
    private readonly IDistributedCache _cache;
    private readonly IPasswordHasher<SysUser> _passwordHasher;
    private readonly IExcelManager _excelManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SystemUserService> _logger;

    public SystemUserService(
        IReadOnlyDbContext readDb,
        IRepository<SysUser> userRepo,
        IRepository<SysUserRole> userRoleRepo,
        ICurrentUser currentUser,
        IRolePermissionService rolePermissionService,
        IDataPermissionService dataPermissionService,
        ITokenManager tokenManager,
        IDistributedCache cache,
        IPasswordHasher<SysUser> passwordHasher,
        IExcelManager excelManager,
        IUnitOfWork unitOfWork,
        ILogger<SystemUserService> logger)
    {
        _readDb = readDb;
        _userRepo = userRepo;
        _userRoleRepo = userRoleRepo;
        _currentUser = currentUser;
        _rolePermissionService = rolePermissionService;
        _dataPermissionService = dataPermissionService;
        _tokenManager = tokenManager;
        _cache = cache;
        _passwordHasher = passwordHasher;
        _excelManager = excelManager;
        _unitOfWork = unitOfWork;
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

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Id == userId.Value && !u.IsDeleted)
                .Select(u => new { u.Id, u.Username, u.Nickname, u.Avatar }),
            cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// 用户分页
    /// </summary>
    public async Task<PageResult<UserPageVo>> GetUserPageAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var (pageNum, pageSize) = query.Normalize();

        var users = _readDb.Query<SysUser>().Where(u => !u.IsDeleted);
        users = ApplyUserFilters(users, query);

        var total = await _readDb.CountAsync(users, cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<UserPageVo>.Success(Array.Empty<UserPageVo>(), 0, pageNum, pageSize);
        }

        var pageRowsQuery =
            from u in users
            join d in _readDb.Query<SysDept>() on u.DeptId equals d.Id into deptJoin
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

        var pageRows = await _readDb.ToListAsync(
            pageRowsQuery
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        // 角色名称需要单独聚合，避免在主查询里引入过多join
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
                CreateTime = x.CreateTime.HasValue ? x.CreateTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
            })
            .ToArray();

        return PageResult<UserPageVo>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 用户表单
    /// </summary>
    public async Task<UserForm> GetUserFormAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Id == userId && !u.IsDeleted)
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
                }),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var roleIds = await _readDb.ToListAsync(
            _readDb.Query<SysUserRole>().Where(ur => ur.UserId == userId).Select(ur => ur.RoleId),
            cancellationToken).ConfigureAwait(false);

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

        var exists = await _readDb.AnyAsync(
            _readDb.Query<SysUser>().Where(u => u.Username == username && !u.IsDeleted),
            cancellationToken).ConfigureAwait(false);

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
            CreateBy = createBy,
            CreateTime = now,
            UpdateBy = createBy,
            UpdateTime = now,
            IsDeleted = false,
        };

        // 哈希基于 BCrypt，不依赖实体字段，签名要求传入用户实例
        user.Password = _passwordHasher.HashPassword(user, DevDefaults.DefaultPassword);

        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        _userRepo.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SaveUserRolesAsync(user.Id, formData.RoleIds, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<bool> UpdateUserAsync(long userId, UserForm formData, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var username = formData.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名不能为空");
        }

        var exists = await _readDb.AnyAsync(
            _readDb.Query<SysUser>().Where(u => u.Id != userId && u.Username == username && !u.IsDeleted),
            cancellationToken).ConfigureAwait(false);

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

        // 角色关系更新与用户更新放在同一事务
        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SaveUserRolesAsync(userId, formData.RoleIds, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    public async Task<bool> DeleteUsersAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = IdCollectionExtensions.ParsePositiveLongIds(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的用户数据为空");
        }

        var users = await _userRepo.ListAsync(u => idList.Contains(u.Id) && !u.IsDeleted, cancellationToken).ConfigureAwait(false);

        if (users.Count == 0)
        {
            return true;
        }

        // 删除时排除超管用户
        var rootUserIdSet = (await GetRootUserIdsAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
        users = users.Where(u => !rootUserIdSet.Contains(u.Id)).ToList();

        if (users.Count == 0)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var updateBy = _currentUser.UserId;
        foreach (var u in users)
        {
            u.IsDeleted = true;
            u.UpdateBy = updateBy;
            u.UpdateTime = now;
        }

        // 一并删除用户的角色关联
        var roles = await _userRoleRepo.ListAsync(ur => idList.Contains(ur.UserId), cancellationToken).ConfigureAwait(false);
        _userRoleRepo.RemoveRange(roles);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Status = status;
                u.UpdateBy = _currentUser.UserId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 解绑手机号（需先校验登录密码）
    /// </summary>
    public async Task<bool> UnbindMobileAsync(PasswordVerifyForm formData, CancellationToken cancellationToken = default)
    {
        var (userId, _) = await VerifyUserPasswordAsync(formData.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Mobile = null;
                u.UpdateBy = userId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 解绑邮箱（需先校验登录密码）
    /// </summary>
    public async Task<bool> UnbindEmailAsync(PasswordVerifyForm formData, CancellationToken cancellationToken = default)
    {
        var (userId, _) = await VerifyUserPasswordAsync(formData.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Email = null;
                u.UpdateBy = userId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

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

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Password = _passwordHasher.HashPassword(u, password);
                u.UpdateBy = _currentUser.UserId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

        if (ok > 0)
        {
            await _tokenManager.InvalidateUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        return ok > 0;
    }

    /// <summary>
    /// 下载导入模板
    /// </summary>
    public async Task<IReadOnlyCollection<byte>> DownloadUserImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        var bytes = await _excelManager.ExportAsync(Array.Empty<UserImportRow>(), UserImportTemplateSpec, cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    /// <summary>
    /// 导出用户
    /// </summary>
    public async Task<IReadOnlyCollection<byte>> ExportUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = _readDb.Query<SysUser>().Where(u => !u.IsDeleted);
        users = ApplyUserFilters(users, query);

        var rowsQuery =
            from u in users
            join d in _readDb.Query<SysDept>() on u.DeptId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            select new
            {
                u.Username,
                u.Nickname,
                u.Mobile,
                u.Email,
                u.Gender,
                DeptName = d != null ? d.Name : null,
                u.CreateTime,
            };

        var rows = await _readDb.ToListAsync(
            rowsQuery.Take(5000),
            cancellationToken).ConfigureAwait(false);

        var genderItems = await _readDb.ToListAsync(
            _readDb.Query<SysDictItem>().Where(i => i.DictCode == "gender" && i.Status == 1)
                .OrderBy(i => i.Sort).ThenBy(i => i.Id),
            cancellationToken).ConfigureAwait(false);
        var genderMap = genderItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Value))
            .ToDictionary(i => i.Value!, i => i.Label ?? string.Empty, StringComparer.Ordinal);

        var exportRows = rows.Select(r => new UserExportRow
        {
            Username = r.Username ?? string.Empty,
            Nickname = r.Nickname ?? string.Empty,
            Dept = r.DeptName ?? string.Empty,
            Gender = r.Gender.HasValue && genderMap.TryGetValue(r.Gender.Value.ToString(), out var genderLabel)
                ? genderLabel
                : string.Empty,
            Mobile = r.Mobile ?? string.Empty,
            Email = r.Email ?? string.Empty,
            CreateTime = r.CreateTime.HasValue
                ? r.CreateTime.Value.ToString("yyyy-MM-dd HH:mm")
                : string.Empty,
        }).ToList();

        var bytes = await _excelManager.ExportAsync(exportRows, UserExportSpec, cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    /// <summary>
    /// 导入用户
    /// </summary>
    public async Task<ExcelResult> ImportUsersAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var valid = 0;
        var invalid = 0;

        // 预加载角色和部门数据（支持编码或名称匹配）
        var roles = await _readDb.ToListAsync(
            _readDb.Query<SysRole>().Where(r => !r.IsDeleted && r.Status == 1)
                .Select(r => new { r.Id, r.Code, r.Name }),
            cancellationToken).ConfigureAwait(false);
        var roleMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in roles)
        {
            if (!string.IsNullOrEmpty(r.Code)) roleMap[r.Code] = r.Id;
            if (!string.IsNullOrEmpty(r.Name)) roleMap[r.Name] = r.Id;
        }

        var depts = await _readDb.ToListAsync(
            _readDb.Query<SysDept>().Where(d => !d.IsDeleted)
                .Select(d => new { d.Id, d.Code, d.Name }),
            cancellationToken).ConfigureAwait(false);
        var deptMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in depts)
        {
            if (!string.IsNullOrEmpty(d.Code)) deptMap[d.Code] = d.Id;
            if (!string.IsNullOrEmpty(d.Name)) deptMap[d.Name] = d.Id;
        }

        var importResult = await _excelManager.ImportAsync(
            content,
            new ExcelImportSpec<UserImportRow>(UserImportTemplateSpec.SheetName, UserImportTemplateSpec.Columns),
            cancellationToken).ConfigureAwait(false);

        var importRows = new List<(SysUser User, List<long> RoleIds)>();
        var excelRow = 2;
        foreach (var row in importResult.Rows)
        {
            var username = row.Username?.Trim();
            var nickname = row.Nickname?.Trim();
            var mobile = row.Mobile?.Trim();
            var email = row.Email?.Trim();
            var roleStr = row.Role?.Trim();
            var deptStr = row.Dept?.Trim();

            if (string.IsNullOrWhiteSpace(username)
                && string.IsNullOrWhiteSpace(nickname)
                && string.IsNullOrWhiteSpace(mobile)
                && string.IsNullOrWhiteSpace(email))
            {
                excelRow++;
                continue;
            }

            // 账号名必填，且不能重复
            if (string.IsNullOrWhiteSpace(username))
            {
                invalid++;
                messages.Add($"第{excelRow}行：用户名不能为空");
                excelRow++;
                continue;
            }

            var exists = await _readDb.AnyAsync(
                _readDb.Query<SysUser>().Where(u => u.Username == username && !u.IsDeleted),
                cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                invalid++;
                messages.Add($"第{excelRow}行：用户名已存在: {username}");
                excelRow++;
                continue;
            }

            // 解析角色
            var roleIds = new List<long>();
            if (!string.IsNullOrEmpty(roleStr))
            {
                foreach (var part in roleStr.Split(',', '，'))
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    if (roleMap.TryGetValue(trimmed, out var roleId))
                    {
                        roleIds.Add(roleId);
                    }
                }
            }

            if (roleIds.Count == 0)
            {
                invalid++;
                messages.Add($"第{excelRow}行：角色不存在或为空");
                excelRow++;
                continue;
            }

            // 解析部门
            long? userDeptId = null;
            if (!string.IsNullOrEmpty(deptStr))
            {
                if (deptMap.TryGetValue(deptStr, out var parsedDeptId))
                {
                    userDeptId = parsedDeptId;
                }
                else
                {
                    invalid++;
                    messages.Add($"第{excelRow}行：部门不存在: {deptStr}");
                    excelRow++;
                    continue;
                }
            }

            var now = DateTime.UtcNow;
            var createBy = _currentUser.UserId;

            var user = new SysUser
            {
                Username = username,
                Nickname = nickname,
                Mobile = mobile,
                Email = email,
                Gender = row.Gender,
                Status = 1,
                DeptId = userDeptId,
                CreateBy = createBy,
                CreateTime = now,
                UpdateBy = createBy,
                UpdateTime = now,
                IsDeleted = false,
            };

            // 哈希基于 BCrypt，不依赖实体字段，签名要求传入用户实例
            user.Password = _passwordHasher.HashPassword(user, DevDefaults.DefaultPassword);

            // 先收集待插入数据，循环结束后再统一提交，避免逐行多次往返数据库
            importRows.Add((user, roleIds.Distinct().ToList()));
            excelRow++;
        }

        // 先批量插入用户（自增 ID 回填），再批量插入用户-角色关联，整批仅两次提交
        foreach (var (user, _) in importRows)
        {
            _userRepo.Add(user);
        }

        if (importRows.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var roleLinks = new List<SysUserRole>();
            foreach (var (user, roleIds) in importRows)
            {
                foreach (var roleId in roleIds)
                {
                    roleLinks.Add(new SysUserRole { UserId = user.Id, RoleId = roleId });
                }
            }

            _userRoleRepo.AddRange(roleLinks);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        valid = importRows.Count;

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
        var userId = _currentUser.GetRequiredUserId();

        var rowQuery =
            from u in _readDb.Query<SysUser>()
            join d in _readDb.Query<SysDept>() on u.DeptId equals d.Id into deptJoin
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

        var row = await _readDb.FirstOrDefaultAsync(rowQuery, cancellationToken).ConfigureAwait(false);
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
            CreateTime = row.CreateTime.HasValue ? row.CreateTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
        };
    }

    /// <summary>
    /// 更新个人资料
    /// </summary>
    public async Task<bool> UpdateProfileAsync(UserProfileForm formData, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();

        var user = await _userRepo.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);

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
            throw new BusinessException(ResultCode.InvalidUserInput, "请至少修改一个字段");
        }

        user.UpdateBy = userId;
        user.UpdateTime = DateTime.UtcNow;

        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(PasswordChangeForm formData, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();

        var user = await _userRepo.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        var oldPwd = formData.OldPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(user.Password)
            || _passwordHasher.VerifyHashedPassword(user, user.Password, oldPwd) != PasswordVerificationResult.Success)
        {
            throw new BusinessException(ResultCode.UserPasswordError, "原密码错误");
        }

        var newPwd = formData.NewPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newPwd))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码不能为空");
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.Password, newPwd) == PasswordVerificationResult.Success)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码不能与原密码相同");
        }

        if (!string.Equals(formData.NewPassword, formData.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "新密码和确认密码不一致");
        }

        user.Password = _passwordHasher.HashPassword(user, newPwd);
        user.UpdateBy = userId;
        user.UpdateTime = DateTime.UtcNow;

        _userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        var code = DevDefaults.VerifyCode;
        _logger.LogInformation("[SendMobileCode] mobile={Mobile}", mobile);

        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        await _cache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VerifyCodeTtl },
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 绑定或修改手机号
    /// </summary>
    public async Task<bool> BindOrChangeMobileAsync(MobileUpdateForm formData, CancellationToken cancellationToken = default)
    {
        var mobile = formData.Mobile?.Trim();
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号不能为空");
        }

        var (userId, _) = await VerifyUserPasswordAsync(formData.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var mobileExists = await _readDb.AnyAsync(
            _readDb.Query<SysUser>().Where(u => !u.IsDeleted && u.Id != userId && u.Mobile == mobile),
            cancellationToken).ConfigureAwait(false);

        if (mobileExists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "手机号已被其他账号绑定");
        }

        var key = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile);
        await VerifyCodeAsync(key, formData.Code ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Mobile = mobile;
                u.UpdateBy = userId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

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

        var code = DevDefaults.VerifyCode;
        _logger.LogInformation("[SendEmailCode] email={Email}", email);

        var key = string.Format(RedisKeyConstants.Captcha.EmailCode, email.Trim());
        await _cache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VerifyCodeTtl },
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 绑定或修改邮箱
    /// </summary>
    public async Task<bool> BindOrChangeEmailAsync(EmailUpdateForm formData, CancellationToken cancellationToken = default)
    {
        var email = formData.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "邮箱不能为空");
        }

        var (userId, _) = await VerifyUserPasswordAsync(formData.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var emailExists = await _readDb.AnyAsync(
            _readDb.Query<SysUser>().Where(u => !u.IsDeleted && u.Id != userId && u.Email == email),
            cancellationToken).ConfigureAwait(false);

        if (emailExists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "邮箱已被其他账号绑定");
        }

        var key = string.Format(RedisKeyConstants.Captcha.EmailCode, email);
        await VerifyCodeAsync(key, formData.Code ?? string.Empty, cancellationToken).ConfigureAwait(false);

        var ok = await _userRepo.BatchUpdateAsync(
            u => u.Id == userId && !u.IsDeleted,
            u =>
            {
                u.Email = email;
                u.UpdateBy = userId;
                u.UpdateTime = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);

        return ok > 0;
    }

    /// <summary>
    /// 用户下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<long>>> ListUserOptionsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _readDb.ToListAsync(
            _readDb.Query<SysUser>()
                .Where(u => !u.IsDeleted && u.Status == 1 && !GetRootUserIdsQuery().Contains(u.Id))
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, u.Nickname, u.Username }),
            cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// 拥有超管角色的用户 ID 查询（排除超管，避免被删除/列表/导出）
    /// </summary>
    private IQueryable<long> GetRootUserIdsQuery()
    {
        return from ur in _readDb.Query<SysUserRole>()
               join r in _readDb.Query<SysRole>() on ur.RoleId equals r.Id
               where !r.IsDeleted && r.Code == SecurityConstants.RootRoleCode
               select ur.UserId;
    }

    private async Task<List<long>> GetRootUserIdsAsync(CancellationToken cancellationToken)
    {
        return await _readDb.ToListAsync(GetRootUserIdsQuery(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 应用用户查询过滤条件（GetUserPageAsync 和 ExportUsersAsync 共用）
    /// </summary>
    private IQueryable<SysUser> ApplyUserFilters(IQueryable<SysUser> users, UserQuery query)
    {
        // 排除超管用户
        users = users.Where(u => !GetRootUserIdsQuery().Contains(u.Id));

        // 数据权限过滤
        users = _dataPermissionService.Apply(users, u => u.DeptId ?? 0, u => u.CreateBy ?? 0);

        // 关键字过滤
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

        var roleIdList = IdCollectionExtensions.ParsePositiveLongIds(query.RoleIds);
        if (roleIdList.Count > 0)
        {
            var roleUserIds = _readDb.Query<SysUserRole>()
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

        return users;
    }

    private async Task<Dictionary<long, string>> GetRoleNamesMapAsync(long[] userIds, CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return new Dictionary<long, string>();
        }

        // 使用 List 替代数组避免 EF Core 9.0 查询编译问题
        var userIdList = userIds.ToList();

        var query =
            from ur in _readDb.Query<SysUserRole>()
            join r in _readDb.Query<SysRole>() on ur.RoleId equals r.Id
            where userIdList.Contains(ur.UserId)
                && !r.IsDeleted
                && r.Status == 1
            select new { ur.UserId, r.Name };

        var rows = await _readDb.ToListAsync(query, cancellationToken).ConfigureAwait(false);

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(",", g.Select(x => x.Name).Distinct(StringComparer.Ordinal))
            );
    }

    private static (DateTime? Start, DateTime? End) ParseTimeRange(IReadOnlyCollection<string>? input)
    {
        if (input is null || input.Count == 0)
        {
            return (null, null);
        }

        if (input.Count != 2)
        {
            return (null, null);
        }

        var parts = input.ToArray();
        var start = TryParseDateTime(parts[0]);
        var end = TryParseDateTime(parts[1]);

        // 结束日期为纯日期(yyyy-MM-dd，长度 10)时，补到 23:59:59，包含当天所有数据
        if (end.HasValue && parts[1].Length == 10)
        {
            end = end.Value.Date.AddDays(1).AddSeconds(-1);
        }

        return (start, end);
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

    private async Task SaveUserRolesAsync(long userId, IReadOnlyCollection<long>? roleIds, CancellationToken cancellationToken)
    {
        var existing = await _userRoleRepo.ListAsync(ur => ur.UserId == userId, cancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            _userRoleRepo.RemoveRange(existing);
        }

        if (roleIds is null || roleIds.Count == 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var distinct = roleIds.Where(r => r > 0).Distinct().ToArray();
        if (distinct.Length == 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        _userRoleRepo.AddRange(distinct.Select(roleId => new SysUserRole { UserId = userId, RoleId = roleId }));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 验证用户存在且密码正确
    /// </summary>
    private async Task<(long UserId, string? Password)> VerifyUserPasswordAsync(
        string password,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Id == userId && !u.IsDeleted),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(user.Password)
            || _passwordHasher.VerifyHashedPassword(user, user.Password, password) != PasswordVerificationResult.Success)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "当前密码错误");
        }

        return (userId, user.Password);
    }

    /// <summary>
    /// 验证缓存中的验证码
    /// </summary>
    private async Task VerifyCodeAsync(string cacheKey, string code, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(cached))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached, code, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        await _cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 用户导出行（与 Excel 列头一一对应）
    /// </summary>
    private sealed class UserExportRow
    {
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Dept { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户导入行（属性名与模板列头映射）
    /// </summary>
    private sealed class UserImportRow
    {
        public string? Username { get; set; }
        public string? Nickname { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? Gender { get; set; }
        public string? Role { get; set; }
        public string? Dept { get; set; }
    }

    private static readonly ExcelSheetSpec UserExportSpec = new("用户列表",
        new[]
        {
            new ExcelColumnSpec("用户名", nameof(UserExportRow.Username)),
            new ExcelColumnSpec("昵称", nameof(UserExportRow.Nickname)),
            new ExcelColumnSpec("部门", nameof(UserExportRow.Dept)),
            new ExcelColumnSpec("性别", nameof(UserExportRow.Gender)),
            new ExcelColumnSpec("手机号", nameof(UserExportRow.Mobile)),
            new ExcelColumnSpec("邮箱", nameof(UserExportRow.Email)),
            new ExcelColumnSpec("创建时间", nameof(UserExportRow.CreateTime)),
        });

    private static readonly ExcelSheetSpec UserImportTemplateSpec = new("用户导入模板",
        new[]
        {
            new ExcelColumnSpec("用户名", nameof(UserImportRow.Username)),
            new ExcelColumnSpec("昵称", nameof(UserImportRow.Nickname)),
            new ExcelColumnSpec("手机号", nameof(UserImportRow.Mobile)),
            new ExcelColumnSpec("邮箱", nameof(UserImportRow.Email)),
            new ExcelColumnSpec("性别", nameof(UserImportRow.Gender)),
            new ExcelColumnSpec("角色", nameof(UserImportRow.Role)),
            new ExcelColumnSpec("部门", nameof(UserImportRow.Dept)),
        });
}
