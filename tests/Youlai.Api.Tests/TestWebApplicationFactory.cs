using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Youlai.Application.Auth.Constants;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;
using Youlai.Api.Controllers;

namespace Youlai.Api.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<UsersController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=127.0.0.1;Port=3306;Database=youlai_admin;Uid=root;Pwd=root;",
                ["Redis:ConnectionString"] = "127.0.0.1:6379",
                ["Security:Session:Jwt:SecretKey"] = "unit-test-secret-key-unit-test-secret-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddSingleton<ISystemUserService, FakeSystemUserService>();
            services.AddSingleton<ISystemMenuService, FakeSystemMenuService>();
            services.AddSingleton<ISystemDeptService, FakeSystemDeptService>();
            services.AddSingleton<ISystemRoleService, FakeSystemRoleService>();
            services.AddSingleton<ISystemDictService, FakeSystemDictService>();
            services.AddSingleton<ISystemNoticeService, FakeSystemNoticeService>();
            services.AddSingleton<IRolePermissionService, FakeRolePermissionService>();
        });
    }

    private sealed class FakeSystemDeptService : ISystemDeptService
    {
        public Task<IReadOnlyCollection<DeptVo>> GetDeptListAsync(DeptQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DeptVo>>(Array.Empty<DeptVo>());
        }

        public Task<IReadOnlyCollection<Option<long>>> GetDeptOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Option<long>>>(Array.Empty<Option<long>>());
        }

        public Task<long> SaveDeptAsync(DeptForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1L);
        }

        public Task<DeptForm> GetDeptFormAsync(long deptId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DeptForm { Id = deptId, ParentId = 0, Status = 1, Sort = 0 });
        }

        public Task<long> UpdateDeptAsync(long deptId, DeptForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(deptId);
        }

        public Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeRolePermissionService : IRolePermissionService
    {
        public Task<IReadOnlyCollection<string>> GetRolePermsAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(new[] { "*:*:*" });
        }
    }

    private sealed class FakeSystemUserService : ISystemUserService
    {
        public Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CurrentUserDto
            {
                UserId = 1,
                Username = "admin",
                Nickname = "管理员",
                Avatar = null,
                Roles = new[] { "ROOT" },
                Perms = new[] { "*:*:*" },
            });
        }

        public Task<PageResult<UserPageVo>> GetUserPageAsync(UserPageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<UserPageVo>.Success(Array.Empty<UserPageVo>(), 0, pageNum, pageSize));
        }

        public Task<UserForm> GetUserFormAsync(long userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new UserForm
            {
                Id = userId,
                Username = "test",
                Nickname = "测试用户",
                DeptId = 1,
                Status = 1,
                RoleIds = Array.Empty<long>(),
            });
        }

        public Task<bool> CreateUserAsync(UserForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateUserAsync(long userId, UserForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteUsersAsync(string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateUserStatusAsync(long userId, int status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ResetUserPasswordAsync(long userId, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<byte>> DownloadUserImportTemplateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<byte>>(Array.Empty<byte>());
        }

        public Task<IReadOnlyCollection<byte>> ExportUsersAsync(UserPageQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<byte>>(Array.Empty<byte>());
        }

        public Task<ExcelResult> ImportUsersAsync(long deptId, Stream content, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExcelResult
            {
                Code = ResultCode.Success.Code(),
                ValidCount = 0,
                InvalidCount = 0,
                MessageList = Array.Empty<string>(),
            });
        }

        public Task<UserProfileVo> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new UserProfileVo { Id = 1 });
        }

        public Task<bool> UpdateProfileAsync(UserProfileForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ChangePasswordAsync(PasswordChangeForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SendMobileCodeAsync(string mobile, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> BindOrChangeMobileAsync(MobileUpdateForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SendEmailCodeAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> BindOrChangeEmailAsync(EmailUpdateForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<Option<long>>> GetUserOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Option<long>>>(Array.Empty<Option<long>>());
        }
    }

    private sealed class FakeSystemMenuService : ISystemMenuService
    {
        public Task<IReadOnlyCollection<RouteVo>> GetCurrentUserRoutesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RouteVo>>(Array.Empty<RouteVo>());
        }

        public Task<IReadOnlyCollection<Option<long>>> GetMenuOptionsAsync(bool onlyParent, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Option<long>>>(Array.Empty<Option<long>>());
        }

        public Task<IReadOnlyCollection<MenuVo>> GetMenuListAsync(MenuQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<MenuVo>>(Array.Empty<MenuVo>());
        }

        public Task<MenuForm> GetMenuFormAsync(long menuId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MenuForm
            {
                Id = menuId,
                ParentId = 0,
                Name = "测试菜单",
                Type = "M",
                Visible = 1,
                Sort = 1,
            });
        }

        public Task<bool> SaveMenuAsync(MenuForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteMenuAsync(long menuId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateMenuVisibleAsync(long menuId, int visible, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeSystemRoleService : ISystemRoleService
    {
        public Task<PageResult<RolePageVo>> GetRolePageAsync(RolePageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<RolePageVo>.Success(Array.Empty<RolePageVo>(), 0, pageNum, pageSize));
        }

        public Task<IReadOnlyCollection<Option<long>>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Option<long>>>(Array.Empty<Option<long>>());
        }

        public Task<bool> SaveRoleAsync(RoleForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<RoleForm> GetRoleFormAsync(long roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RoleForm
            {
                Id = roleId,
                Name = "测试角色",
                Code = "TEST",
                Sort = 0,
                Status = 1,
            });
        }

        public Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateRoleStatusAsync(long roleId, int status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<long>> GetRoleMenuIdsAsync(long roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<long>>(Array.Empty<long>());
        }

        public Task AssignMenusToRoleAsync(long roleId, IReadOnlyCollection<long> menuIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSystemDictService : ISystemDictService
    {
        public Task<PageResult<DictPageVo>> GetDictPageAsync(DictPageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<DictPageVo>.Success(Array.Empty<DictPageVo>(), 0, pageNum, pageSize));
        }

        public Task<IReadOnlyCollection<Option<string>>> GetDictListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Option<string>>>(Array.Empty<Option<string>>());
        }

        public Task<DictForm> GetDictFormAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DictForm { Id = id, DictCode = "gender", Name = "性别", Status = 1 });
        }

        public Task<bool> CreateDictAsync(DictForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateDictAsync(long id, DictForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteDictsAsync(string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<PageResult<DictItemPageVo>> GetDictItemPageAsync(string dictCode, DictItemPageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<DictItemPageVo>.Success(Array.Empty<DictItemPageVo>(), 0, pageNum, pageSize));
        }

        public Task<IReadOnlyCollection<DictItemOption>> GetDictItemsAsync(string dictCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DictItemOption>>(Array.Empty<DictItemOption>());
        }

        public Task<DictItemForm> GetDictItemFormAsync(string dictCode, long itemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DictItemForm { Id = itemId, DictCode = dictCode, Label = "男", Value = "1", Status = 1, Sort = 1 });
        }

        public Task<bool> CreateDictItemAsync(string dictCode, DictItemForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateDictItemAsync(string dictCode, long itemId, DictItemForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteDictItemsAsync(string dictCode, string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeSystemNoticeService : ISystemNoticeService
    {
        public Task<PageResult<NoticePageVo>> GetNoticePageAsync(NoticePageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize));
        }

        public Task<NoticeForm> GetNoticeFormAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NoticeForm { Id = id, Title = "测试", Content = "<p>内容</p>", TargetType = 1, Level = "L", Type = 1 });
        }

        public Task<bool> CreateNoticeAsync(NoticeForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateNoticeAsync(long id, NoticeForm formData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteNoticesAsync(string ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> PublishNoticeAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RevokeNoticeAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<NoticeDetailVo> GetNoticeDetailAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NoticeDetailVo { Id = id, Title = "测试" });
        }

        public Task<bool> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<PageResult<NoticePageVo>> GetMyNoticePageAsync(NoticePageQuery query, CancellationToken cancellationToken = default)
        {
            var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            return Task.FromResult(PageResult<NoticePageVo>.Success(Array.Empty<NoticePageVo>(), 0, pageNum, pageSize));
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(JwtClaimConstants.UserId, "1"),
                new(JwtClaimConstants.DeptId, "1"),
                new(JwtClaimConstants.DataScope, ((int)DataScope.All).ToString()),
                new(JwtClaimConstants.Authorities, SecurityConstants.RolePrefix + SecurityConstants.RootRoleCode),
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
