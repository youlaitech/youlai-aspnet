using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Application.Auth;
using Youlai.Application.Codegen;
using Youlai.Application.Options;
using Youlai.Application.Persistence;
using Youlai.Application.Security;
using Youlai.Application.Common;
using Youlai.Application.File;
using Youlai.Application.Form;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Auth;
using Youlai.Infrastructure.Codegen;
using Youlai.Infrastructure.Form;
using Youlai.Infrastructure.Common;
using Youlai.Infrastructure.FileStorage;
using Youlai.Infrastructure.Persistence;
using Youlai.Infrastructure.Security;

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

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Type), "FileStorage:Type is required")
            .ValidateOnStart();

        services.AddOptions<WxMaOptions>()
            .Bind(configuration.GetSection(WxMaOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FileStorageOptions>>().Value);

        // MySQL 服务器版本：启动时探测一次，避免每次请求重复连接探测
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
        var serverVersion = ServerVersion.AutoDetect(databaseOptions?.ConnectionString!);

        // 数据库上下文
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseMySql(dbOptions.ConnectionString, serverVersion);
        });
        // 工作单元与仓储
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IReadOnlyDbContext, EfReadOnlyDbContext>();
        services.AddScoped<IExcelManager, ClosedXmlExcelManager>();
        services.AddScoped<ISqlExecutor, EfSqlExecutor>();
        services.AddScoped<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddScoped<IPasswordHasher<SysUser>, BCryptPasswordHasher>();

        // Redis 连接（令牌管理与缓存共用）
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        // IDistributedCache（AuthService 短信验证码等场景使用）
        services.AddStackExchangeRedisCache(opt =>
        {
            var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
            opt.Configuration = redisOptions?.ConnectionString ?? string.Empty;
        });

        // 验证码服务
        services.AddScoped<ICaptchaService, CaptchaService>();

        // JWT / 令牌管理
        services.AddScoped<ITokenManager, JwtTokenManager>();

        // 文件存储：按 Type 路由到对应的存储实现
        services.AddScoped<IFileStorage>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            return options.Type switch
            {
                "s3" => new S3FileStorage(options),
                "aliyun" => new AliyunFileStorage(options),
                _ => new LocalFileStorage(options)
            };
        });

        // HTTP 客户端
        services.AddHttpClient("Wechat");
        services.AddHttpClient("Ai");

        // 公开表单提交限流
        services.AddSingleton<IPublicSubmitLimiter, RedisPublicSubmitLimiter>();

        return services;
    }
}
