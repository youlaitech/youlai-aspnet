using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Persistence;
using Youlai.Application.Options;
using Youlai.Application.Security;
using Youlai.Application.File;
using Youlai.Infrastructure.FileStorage;
using Youlai.Infrastructure.Persistence;

namespace Youlai.Infrastructure;

/// <summary>
/// 基础设施层服务注册（DbContext、存储、Redis 等）
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册基础设施层服务（DbContext、存储、Redis 等）
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 配置项
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

        services.AddOptions<WxMaOptions>()
            .Bind(configuration.GetSection(WxMaOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OssOptions>>().Value);

        // 数据库上下文
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseMySql(dbOptions.ConnectionString, ServerVersion.AutoDetect(dbOptions.ConnectionString));
        });
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        // JWT / 令牌管理
        services.AddScoped<JwtTokenManager>();

        // 文件存储：按 OssOptions.Type 选择具体实现（策略模式）
        services.AddScoped<IFileStorage>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OssOptions>>().Value;
            return options.Type switch
            {
                "minio" => new MinioFileStorage(options),
                "aliyun" => new AliyunFileStorage(options),
                _ => new LocalFileStorage(options)
            };
        });

        // HTTP 客户端
        services.AddHttpClient("Wechat");

        return services;
    }
}
