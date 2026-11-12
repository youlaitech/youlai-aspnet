using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Youlai.Api.Converters;
using Youlai.Api.Security;
using Youlai.Api.Middlewares;
using Youlai.Api.Filters;
using Youlai.Api.Realtime;
using Youlai.Application;
using Youlai.Application.Security;
using Youlai.Application.Results;
using Youlai.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddService<LogActionFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "参数校验失败" : e.ErrorMessage)
                .ToArray();

            var msg = string.Join("；", errors);
            var body = Result.Failed(ResultCode.InvalidUserInput, msg);
            return new BadRequestObjectResult(body);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new Int64ToStringJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableInt64ToStringJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeToStringJsonConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "youlai-aspnet",
        Description = "youlai 权限管理平台 RESTful API",
        Version = "1.0",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.OperationFilter<Youlai.Api.Swagger.FileUploadOperationFilter>();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// 跨域来源白名单（Cors:AllowedOrigins），未配置时仅放行本地开发前端
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:5173", "http://127.0.0.1:5173"];
}
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddScoped<LogActionFilter>();

var app = builder.Build();

// 配置 HTTP 请求管道
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 先处理跨域预检，避免 OPTIONS 被后续限流与认证拦截
app.UseCors();

// 本地存储目录以静态文件方式对外提供访问
var localStoragePath = app.Configuration["FileStorage:Local:StoragePath"] ?? "uploads";
var localStorageRoot = Path.IsPathRooted(localStoragePath)
    ? localStoragePath
    : Path.Combine(app.Environment.ContentRootPath, localStoragePath);
if (Directory.Exists(localStorageRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(localStorageRoot)),
    });
}

app.UseMiddleware<RateLimitMiddleware>();

app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    var response = httpContext.Response;

    if (response.HasStarted)
    {
        return;
    }

    var code = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ResultCode.AccessTokenInvalid,
        StatusCodes.Status403Forbidden => ResultCode.AccessUnauthorized,
        StatusCodes.Status404NotFound => ResultCode.InterfaceNotExist,
        _ => ResultCode.SystemError,
    };

    response.ContentType = "application/json; charset=utf-8";
    var result = Result.Failed(code);
    await response.WriteAsJsonAsync(result);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Youlai.Api v1");
        options.DocumentTitle = "youlai-aspnet API 文档";
        options.ConfigObject = new ConfigObject
        {
            AdditionalItems =
            {
                ["tagsSorter"] = "alpha",
            },
        };
    });
}

// 实时连接走 WebSocket，握手在中间件内用一次性票据完成
app.UseWebSockets();
app.UseMiddleware<RealtimeWebSocketMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 启动成功提示
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var addresses = app.Urls.ToArray();
if (addresses.Length > 0)
{
    var address = addresses[0];
    logger.LogInformation(
        "============================================================\n" +
        "              youlai-aspnet 启动成功\n" +
        "------------------------------------------------------------\n" +
        "  应用地址: {Address}\n" +
        "  API 文档: {Address}/swagger\n" +
        "============================================================",
        address, address);
}

try
{
    app.Run();
}
catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode is System.Net.Sockets.SocketError.AccessDenied
             or System.Net.Sockets.SocketError.AddressAlreadyInUse)
{
    logger.LogError(
        "============================================================\n" +
        "                    端口绑定失败\n" +
        "------------------------------------------------------------\n" +
        "  监听地址绑定被拒绝，端口可能已被占用或当前用户无绑定权限\n" +
        "  请检查:\n" +
        "  1. 是否有其他程序占用配置的端口\n" +
        "  2. 用 netstat -ano 查看端口占用进程\n" +
        "  3. 结束占用进程或修改监听地址后重试\n" +
        "============================================================");
    throw;
}

/// <summary>
/// 应用入口：注册服务、中间件与 HTTP 请求管道
/// </summary>
public partial class Program
{
}
