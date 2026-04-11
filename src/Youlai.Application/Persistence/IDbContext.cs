using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Youlai.Domain.Entities;

namespace Youlai.Application.Persistence;

/// <summary>
/// DbContext 接口，用于依赖反转
/// </summary>
public interface IDbContext
{
    DatabaseFacade Database { get; }
    DbSet<SysUser> SysUsers { get; }
    DbSet<SysRole> SysRoles { get; }
    DbSet<SysUserRole> SysUserRoles { get; }
    DbSet<SysDept> SysDepts { get; }
    DbSet<SysMenu> SysMenus { get; }
    DbSet<SysRoleMenu> SysRoleMenus { get; }
    DbSet<SysRoleDept> SysRoleDepts { get; }
    DbSet<SysDict> SysDicts { get; }
    DbSet<SysDictItem> SysDictItems { get; }
    DbSet<SysNotice> SysNotices { get; }
    DbSet<SysUserNotice> SysUserNotices { get; }
    DbSet<SysConfig> SysConfigs { get; }
    DbSet<SysLog> SysLogs { get; }
    DbSet<SysUserSocial> SysUserSocials { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
