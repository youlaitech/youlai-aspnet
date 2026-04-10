using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Persistence;
using Youlai.Application.Options;
using Youlai.Application.Security;
using Youlai.Application.File.Interfaces;
using Youlai.Infrastructure.FileStorage;
using Youlai.Infrastructure.Persistence;

namespace Youlai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
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

        services.AddOptions<OssOptions>()
            .Bind(configuration.GetSection(OssOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Type), "Oss:Type is required")
            .ValidateOnStart();

        services.AddOptions<WechatMiniappOptions>()
            .Bind(configuration.GetSection(WechatMiniappOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OssOptions>>().Value);

        // DbContext
        services.AddDbContext<YoulaiDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseMySql(dbOptions.ConnectionString, ServerVersion.AutoDetect(dbOptions.ConnectionString));
        });
        services.AddScoped<IYoulaiDbContext>(sp => sp.GetRequiredService<YoulaiDbContext>());

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        // Identity
        services.AddScoped<JwtTokenManager>();

        // File Storage (strategy pattern)
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IFileStorage, MinioFileStorage>();
        services.AddScoped<IFileStorage, AliyunFileStorage>();

        // Http clients
        services.AddHttpClient("Wechat");

        return services;
    }
}
