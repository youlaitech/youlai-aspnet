using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;
using Youlai.Infrastructure.Data;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 部门服务
/// </summary>
/// <remarks>
/// 提供部门树查询、下拉选项与部门维护能力
/// </remarks>
internal sealed class SystemDeptService : ISystemDeptService
{
    private const long RootNodeId = 0;

    private sealed record DeptOptionRow(long Id, long ParentId, string Name);

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SystemDeptService(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 部门列表
    /// </summary>
    public async Task<IReadOnlyCollection<DeptVo>> GetDeptListAsync(DeptQuery query, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.SysDepts
            .AsNoTracking()
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            q = q.Where(d => d.Name.Contains(keywords));
        }

        if (query.Status.HasValue)
        {
            q = q.Where(d => d.Status == query.Status.Value);
        }

        var list = await q
            .OrderBy(d => d.Sort ?? 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (list.Count == 0)
        {
            return Array.Empty<DeptVo>();
        }

        var deptIds = list.Select(d => d.Id).ToHashSet();
        var parentIds = list.Select(d => d.ParentId).ToHashSet();
        var rootIds = parentIds.Where(pid => !deptIds.Contains(pid)).ToArray();

        var result = new List<DeptVo>();
        foreach (var rootId in rootIds)
        {
            result.AddRange(RecurDeptList(rootId, list));
        }

        return result;
    }

    /// <summary>
    /// 部门下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<long>>> GetDeptOptionsAsync(CancellationToken cancellationToken = default)
    {
        var q = _dbContext.SysDepts
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == 1);

        var list = await q
            .OrderBy(d => d.Sort ?? 0)
            .Select(d => new DeptOptionRow(d.Id, d.ParentId, d.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (list.Count == 0)
        {
            return Array.Empty<Option<long>>();
        }

        var deptIds = list.Select(d => d.Id).ToHashSet();
        var parentIds = list.Select(d => d.ParentId).ToHashSet();
        var rootIds = parentIds.Where(pid => !deptIds.Contains(pid)).ToArray();

        var result = new List<Option<long>>();
        foreach (var rootId in rootIds)
        {
            result.AddRange(RecurDeptOptions(rootId, list));
        }

        return result;
    }

    /// <summary>
    /// 新增部门
    /// </summary>
    public async Task<long> SaveDeptAsync(DeptForm formData, CancellationToken cancellationToken = default)
    {
        var parentId = formData.ParentId ?? RootNodeId;

        if (string.IsNullOrWhiteSpace(formData.Code))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "部门编号不能为空");
        }

        var code = formData.Code.Trim();
        var exists = await _dbContext.SysDepts
            .AsNoTracking()
            .AnyAsync(d => !d.IsDeleted && d.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "部门编号已存在");
        }

        var entity = new SysDept
        {
            Name = formData.Name?.Trim() ?? string.Empty,
            Code = code,
            ParentId = parentId,
            Sort = (short?)(formData.Sort ?? 0),
            Status = formData.Status ?? 1,
            TreePath = await GenerateDeptTreePathAsync(parentId, cancellationToken).ConfigureAwait(false),
            CreateBy = _currentUser.UserId,
            CreateTime = DateTime.Now,
            IsDeleted = false,
        };

        _dbContext.SysDepts.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }

    /// <summary>
    /// 部门表单
    /// </summary>
    public async Task<DeptForm> GetDeptFormAsync(long deptId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysDepts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deptId && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new DeptForm();
        }

        return new DeptForm
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            ParentId = entity.ParentId,
            Status = entity.Status,
            Sort = entity.Sort.HasValue ? entity.Sort.Value : null,
        };
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public async Task<long> UpdateDeptAsync(long deptId, DeptForm formData, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysDepts
            .FirstOrDefaultAsync(d => d.Id == deptId && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "部门不存在");
        }

        var parentId = formData.ParentId ?? RootNodeId;

        if (string.IsNullOrWhiteSpace(formData.Code))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "部门编号不能为空");
        }

        var code = formData.Code.Trim();
        var exists = await _dbContext.SysDepts
            .AsNoTracking()
            .AnyAsync(d => !d.IsDeleted && d.Id != deptId && d.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "部门编号已存在");
        }

        entity.Name = formData.Name?.Trim() ?? string.Empty;
        entity.Code = code;
        entity.ParentId = parentId;
        entity.Sort = (short?)(formData.Sort ?? 0);
        entity.Status = formData.Status ?? entity.Status;
        entity.TreePath = await GenerateDeptTreePathAsync(parentId, cancellationToken).ConfigureAwait(false);
        entity.UpdateBy = _currentUser.UserId;
        entity.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }

    /// <summary>
    /// 批量删除部门
    /// </summary>
    public async Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return true;
        }

        var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var deptIds = parts
            .Select(p => long.TryParse(p, out var v) ? v : 0)
            .Where(v => v > 0)
            .Distinct()
            .ToArray();

        if (deptIds.Length == 0)
        {
            return true;
        }

        foreach (var deptId in deptIds)
        {
            var deptIdStr = deptId.ToString();
            var patten = "%," + deptIdStr + ",%";

            var matched = await _dbContext.SysDepts
                .Where(d => d.Id == deptId
                    || EF.Functions.Like(string.Concat(",", d.TreePath, ","), patten))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var d in matched)
            {
                d.IsDeleted = true;
                d.UpdateBy = _currentUser.UserId;
                d.UpdateTime = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private List<DeptVo> RecurDeptList(long parentId, List<SysDept> deptList)
    {
        return deptList
            .Where(d => d.ParentId == parentId)
            .Select(d => new DeptVo
            {
                Id = d.Id,
                ParentId = d.ParentId,
                Name = d.Name,
                Code = d.Code,
                Sort = d.Sort.HasValue ? d.Sort.Value : null,
                Status = d.Status,
                CreateTime = d.CreateTime.HasValue ? d.CreateTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
                UpdateTime = d.UpdateTime.HasValue ? d.UpdateTime.Value.ToString("yyyy-MM-dd HH:mm") : null,
                Children = RecurDeptList(d.Id, deptList),
            })
            .ToList();
    }

    private static List<Option<long>> RecurDeptOptions(long parentId, IReadOnlyCollection<DeptOptionRow> deptList)
    {
        var list = new List<Option<long>>();
        foreach (var d in deptList)
        {
            if (d.ParentId != parentId)
            {
                continue;
            }

            var children = RecurDeptOptions(d.Id, deptList);
            list.Add(new Option<long>(d.Id, d.Name)
            {
                Children = children.Count == 0 ? null : children,
            });
        }

        return list;
    }

    private async Task<string> GenerateDeptTreePathAsync(long parentId, CancellationToken cancellationToken)
    {
        if (parentId == RootNodeId)
        {
            return RootNodeId.ToString();
        }

        var parent = await _dbContext.SysDepts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == parentId && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return parent is null ? RootNodeId.ToString() : parent.TreePath + "," + parent.Id;
    }
}
