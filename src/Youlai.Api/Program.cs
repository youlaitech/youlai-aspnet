using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Youlai.Api.Converters;
using Youlai.Api.Security;
using Youlai.Api.Middlewares;
using Youlai.Application;
using Youlai.Infrastructure.Common.Filters;
using Youlai.Core.Security;
using Youlai.Core.Results;
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
        options.JsonSerializerOptions.Converters.Add(new Int64ToStringJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableInt64ToStringJsonConverter());
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

builder.Services.AddScoped<LogActionFilter>();

var app = builder.Build();

// 配置 HTTP 请求管道
app.UseMiddleware<ExceptionHandlingMiddleware>();
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

app.UseHttpsRedirection();

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
catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AccessDenied)
{
    logger.LogError(
        "============================================================\n" +
        "                    端口绑定失败\n" +
        "------------------------------------------------------------\n" +
        "  端口 8000 已被占用或权限不足\n" +
        "  请检查:\n" +
        "  1. 是否有其他程序占用该端口\n" +
        "  2. 使用 netstat -ano | findstr :8000 查看占用进程\n" +
        "  3. 结束占用进程或更换端口后重试\n" +
        "============================================================");
    throw;
}

public partial class Program
{
}
