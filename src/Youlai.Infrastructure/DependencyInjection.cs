using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Persistence;
using Youlai.Application.Options;
using Youlai.Application.Security;
using Youlai.Application.File;
using Youlai.Application.Common;
using Youlai.Infrastructure.Common;
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

        // 端口 ↔ 实现绑定（与旧 IDbContext 并存，业务逐步迁移到下列抽象）
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IReadOnlyDbContext, EfReadOnlyDbContext>();
        services.AddScoped<IExcelManager, ClosedXmlExcelManager>();

        // Redis（直接类型仍供 JwtTokenManager 使用，待令牌管理迁移到 ITokenManager 后移除）
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        // 分布式缓存：业务通过 IDistributedCache 使用，不依赖 StackExchange.Redis 具体类型
        // 不设置 InstanceName，键名与原有直连 Redis 保持一致
        services.AddStackExchangeRedisCache(opt => opt.Configuration = redisOptions.ConnectionString);

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
