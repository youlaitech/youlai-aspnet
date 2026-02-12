using Microsoft.EntityFrameworkCore;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Persistence.DbContext;

internal sealed class YoulaiDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public YoulaiDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<YoulaiDbContext> options)
        : base(options)
    {
    }

    public DbSet<SysUser> SysUsers => Set<SysUser>();

    public DbSet<SysRole> SysRoles => Set<SysRole>();

    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();

    public DbSet<SysDept> SysDepts => Set<SysDept>();

    public DbSet<SysMenu> SysMenus => Set<SysMenu>();

    public DbSet<SysRoleMenu> SysRoleMenus => Set<SysRoleMenu>();

    public DbSet<SysDict> SysDicts => Set<SysDict>();

    public DbSet<SysDictItem> SysDictItems => Set<SysDictItem>();

    public DbSet<SysNotice> SysNotices => Set<SysNotice>();

    public DbSet<SysUserNotice> SysUserNotices => Set<SysUserNotice>();

    public DbSet<SysConfig> SysConfigs => Set<SysConfig>();

    public DbSet<SysLog> SysLogs => Set<SysLog>();

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
            entity.Property(e => e.OpenId).HasColumnName("openid");
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
        });

        modelBuilder.Entity<SysMenu>(entity =>
        {
            entity.ToTable("sys_menu");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.TreePath).HasColumnName("tree_path");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.RouteName).HasColumnName("route_name");
            entity.Property(e => e.RoutePath).HasColumnName("route_path");
            entity.Property(e => e.Component).HasColumnName("component");
            entity.Property(e => e.Perm).HasColumnName("perm");
            entity.Property(e => e.AlwaysShow).HasColumnName("always_show");
            entity.Property(e => e.KeepAlive).HasColumnName("keep_alive");
            entity.Property(e => e.Visible).HasColumnName("visible");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Icon).HasColumnName("icon");
            entity.Property(e => e.Redirect).HasColumnName("redirect");
            entity.Property(e => e.Params).HasColumnName("params");
        });

        modelBuilder.Entity<SysRoleMenu>(entity =>
        {
            entity.ToTable("sys_role_menu");
            entity.HasKey(e => new { e.RoleId, e.MenuId });

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.MenuId).HasColumnName("menu_id");
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
        });

        modelBuilder.Entity<SysLog>(entity =>
        {
            entity.ToTable("sys_log");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Module).HasColumnName("module");
            entity.Property(e => e.RequestMethod).HasColumnName("request_method");
            entity.Property(e => e.RequestParams).HasColumnName("request_params");
            entity.Property(e => e.ResponseContent).HasColumnName("response_content");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.RequestUri).HasColumnName("request_uri");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.Ip).HasColumnName("ip");
            entity.Property(e => e.Province).HasColumnName("province");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");
            entity.Property(e => e.Browser).HasColumnName("browser");
            entity.Property(e => e.BrowserVersion).HasColumnName("browser_version");
            entity.Property(e => e.Os).HasColumnName("os");
            entity.Property(e => e.CreateBy).HasColumnName("create_by");
            entity.Property(e => e.CreateTime).HasColumnName("create_time");
        });
    }
}
