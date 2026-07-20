using Microsoft.EntityFrameworkCore;
using Youlai.Domain.Entities;
using Youlai.Domain;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Youlai.Application.Security;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// 应用数据库上下文（EF Core）：实体映射与全局过滤
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(
        Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext> options,
        ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>用户表</summary>
    public DbSet<SysUser> SysUsers => Set<SysUser>();

    /// <summary>角色表</summary>
    public DbSet<SysRole> SysRoles => Set<SysRole>();

    /// <summary>用户-角色关联表</summary>
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();

    /// <summary>部门表</summary>
    public DbSet<SysDept> SysDepts => Set<SysDept>();

    /// <summary>菜单表</summary>
    public DbSet<SysMenu> SysMenus => Set<SysMenu>();

    /// <summary>角色-菜单关联表</summary>
    public DbSet<SysRoleMenu> SysRoleMenus => Set<SysRoleMenu>();

    /// <summary>角色-部门关联表</summary>
    public DbSet<SysRoleDept> SysRoleDepts => Set<SysRoleDept>();

    /// <summary>字典表</summary>
    public DbSet<SysDict> SysDicts => Set<SysDict>();

    /// <summary>字典项表</summary>
    public DbSet<SysDictItem> SysDictItems => Set<SysDictItem>();

    /// <summary>通知公告表</summary>
    public DbSet<SysNotice> SysNotices => Set<SysNotice>();

    /// <summary>用户-公告阅读记录表</summary>
    public DbSet<SysUserNotice> SysUserNotices => Set<SysUserNotice>();

    /// <summary>系统配置表</summary>
    public DbSet<SysConfig> SysConfigs => Set<SysConfig>();

    /// <summary>操作日志表</summary>
    public DbSet<SysLog> SysLogs => Set<SysLog>();

    /// <summary>第三方账号绑定表</summary>
    public DbSet<SysUserSocial> SysUserSocials => Set<SysUserSocial>();

    /// <summary>
    /// 配置实体到表/列的映射；带 is_deleted 的表统一加逻辑删除过滤，查询默认只返回未删除数据
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SysUser>(entity =>
        {
            entity.ToTable("sys_user");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.Nickname).HasColumnName("nickname");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.Mobile).HasColumnName("mobile");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.ToTable("sys_role");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.DataScope).HasColumnName("data_scope");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysUserRole>(entity =>
        {
            entity.ToTable("sys_user_role");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<SysDept>(entity =>
        {
            entity.ToTable("sys_dept");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.TreePath).HasColumnName("tree_path");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysMenu>(entity =>
        {
            entity.ToTable("sys_menu");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.TreePath).HasColumnName("tree_path");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(1).IsRequired();
            entity.Property(e => e.RouteName).HasColumnName("route_name").HasMaxLength(255);
            entity.Property(e => e.RoutePath).HasColumnName("route_path").HasMaxLength(128);
            entity.Property(e => e.Component).HasColumnName("component").HasMaxLength(128);
            entity.Property(e => e.ExternalUrl).HasColumnName("external_url").HasMaxLength(512);
            entity.Property(e => e.Perm).HasColumnName("perm").HasMaxLength(128);
            entity.Property(e => e.AlwaysShow).HasColumnName("always_show");
            entity.Property(e => e.KeepAlive).HasColumnName("keep_alive");
            entity.Property(e => e.Visible).HasColumnName("visible");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(64);
            entity.Property(e => e.Redirect).HasColumnName("redirect").HasMaxLength(128);
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            // params 列在库中存 JSON 文本，实体侧用 JsonElement
            entity.Property(e => e.Params)
                .HasColumnName("params")
                .HasConversion(
                    v => v.HasValue ? v.Value.GetRawText() : null,
                    v => ParseJsonElement(v));
        });

        modelBuilder.Entity<SysRoleMenu>(entity =>
        {
            entity.ToTable("sys_role_menu");
            entity.HasKey(e => new { e.RoleId, e.MenuId });

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.MenuId).HasColumnName("menu_id");
        });

        modelBuilder.Entity<SysRoleDept>(entity =>
        {
            entity.ToTable("sys_role_dept");
            entity.HasKey(e => new { e.RoleId, e.DeptId });

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
        });

        modelBuilder.Entity<SysDict>(entity =>
        {
            entity.ToTable("sys_dict");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DictCode).HasColumnName("dict_code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysDictItem>(entity =>
        {
            entity.ToTable("sys_dict_item");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DictCode).HasColumnName("dict_code");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Label).HasColumnName("label");
            entity.Property(e => e.TagType).HasColumnName("tag_type");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
        });

        modelBuilder.Entity<SysNotice>(entity =>
        {
            entity.ToTable("sys_notice");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.TargetType).HasColumnName("target_type");
            entity.Property(e => e.TargetUserIds).HasColumnName("target_user_ids");
            entity.Property(e => e.PublisherId).HasColumnName("publisher_id");
            entity.Property(e => e.PublishStatus).HasColumnName("publish_status");
            entity.Property(e => e.PublishTime).HasColumnName("publish_time");
            entity.Property(e => e.RevokeTime).HasColumnName("revoke_time");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysUserNotice>(entity =>
        {
            entity.ToTable("sys_user_notice");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NoticeId).HasColumnName("notice_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReadTime).HasColumnName("read_time");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysConfig>(entity =>
        {
            entity.ToTable("sys_config");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConfigName).HasColumnName("config_name");
            entity.Property(e => e.ConfigKey).HasColumnName("config_key");
            entity.Property(e => e.ConfigValue).HasColumnName("config_value");
            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");
            entity.Property(e => e.UpdateBy).HasColumnName("update_by");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<SysLog>(entity =>
        {
            entity.ToTable("sys_log");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Module).HasColumnName("module");
            entity.Property(e => e.ActionType).HasColumnName("action_type");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.OperatorId).HasColumnName("operator_id");
            entity.Property(e => e.OperatorName).HasColumnName("operator_name");
            entity.Property(e => e.RequestUri).HasColumnName("request_uri");
            entity.Property(e => e.RequestMethod).HasColumnName("request_method");
            entity.Property(e => e.Ip).HasColumnName("ip");
            entity.Property(e => e.Province).HasColumnName("province");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Device).HasColumnName("device");
            entity.Property(e => e.Os).HasColumnName("os");
            entity.Property(e => e.Browser).HasColumnName("browser");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.ErrorMsg).HasColumnName("error_msg");
            entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
        });

        modelBuilder.Entity<SysUserSocial>(entity =>
        {
            entity.ToTable("sys_user_social");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Platform).HasColumnName("platform");
            entity.Property(e => e.OpenId).HasColumnName("openid");
            entity.Property(e => e.UnionId).HasColumnName("unionid");
            entity.Property(e => e.Nickname).HasColumnName("nickname");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.SessionKey).HasColumnName("session_key");
            entity.Property(e => e.Verified).HasColumnName("verified");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
            entity.Property(e => e.UpdateTime).HasColumnName("update_time");

            entity.HasIndex(e => new { e.Platform, e.OpenId }).HasDatabaseName("uk_platform_openid").IsUnique();
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
            entity.HasIndex(e => e.UnionId).HasDatabaseName("idx_unionid");
        });
    }

    /// <summary>
    /// JSON 文本转 JsonElement，非法 JSON 返回 null
    /// </summary>
    private static JsonElement? ParseJsonElement(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        try
        {
            return JsonDocument.Parse(value).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 保存时统一填充审计字段（创建/更新人、时间）；仅补未显式赋值的字段，避免覆盖业务代码设置
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void StampAudit()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<IHasAudit>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateTime ??= now;
                    entry.Entity.CreateBy ??= userId;
                    entry.Entity.UpdateTime ??= now;
                    entry.Entity.UpdateBy ??= userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateTime ??= now;
                    entry.Entity.UpdateBy ??= userId;
                    break;
            }
        }
    }
}
