using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Services;
using Youlai.Application.System.Services;
using Youlai.Infrastructure.Data;
using Youlai.Infrastructure.Options;
using Youlai.Infrastructure.Services;
using Youlai.Infrastructure.WebSockets;

namespace Youlai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is required")
            .ValidateOnStart();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis:ConnectionString is required")
            .ValidateOnStart();

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Session.Jwt.SecretKey), "Security:Session:Jwt:SecretKey is required")
            .ValidateOnStart();

        services.AddOptions<CaptchaOptions>()
            .Bind(configuration.GetSection(CaptchaOptions.SectionName))
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseMySql(dbOptions.ConnectionString, ServerVersion.AutoDetect(dbOptions.ConnectionString));
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        services.AddScoped<ICaptchaService, CaptchaService>();

        services.AddScoped<JwtTokenManager>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ISystemUserService, SystemUserService>();
        services.AddScoped<ISystemMenuService, SystemMenuService>();
        services.AddScoped<ISystemDeptService, SystemDeptService>();
        services.AddScoped<ISystemRoleService, SystemRoleService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<ISystemDictService, SystemDictService>();
        services.AddScoped<ISystemNoticeService, SystemNoticeService>();

        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IRolePermsCacheInvalidator, RolePermsCacheInvalidator>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        services.AddSingleton<StompBroker>();
        services.AddScoped<IWebSocketService, StompWebSocketService>();

        return services;
    }
}
