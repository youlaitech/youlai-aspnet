using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using StackExchange.Redis;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Services;
using Youlai.Application.Platform.File.Services;
using Youlai.Application.Platform.Codegen.Services;
using Youlai.Application.Platform.Ai.Services;
using Youlai.Application.System.Services;
using Youlai.Infrastructure.Persistence.DbContext;
using Youlai.Infrastructure.Options;
using Youlai.Infrastructure.Services;
using Youlai.Infrastructure.Services.File;
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

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "AI:BaseUrl is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "AI:ApiKey is required")
            .ValidateOnStart();

        services.AddOptions<OssOptions>()
            .Bind(configuration.GetSection(OssOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Type), "Oss:Type is required")
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OssOptions>>().Value);

        services.AddDbContext<YoulaiDbContext>((sp, options) =>
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
        services.AddScoped<ICodegenService, CodegenService>();

        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IFileStorage, AliyunFileStorage>();

        services.AddScoped<IAiAssistantService, AiAssistantService>();

        services.AddSingleton<IChatCompletionService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var baseUrl = options.BaseUrl.TrimEnd('/');
            if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = $"{baseUrl}/v1";
            }

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs),
            };

            return new OpenAIChatCompletionService(options.Model, options.ApiKey, httpClient: httpClient);
        });

        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IRolePermsCacheInvalidator, RolePermsCacheInvalidator>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        services.AddSingleton<StompBroker>();
        services.AddScoped<IWebSocketService, StompWebSocketService>();

        return services;
    }
}
