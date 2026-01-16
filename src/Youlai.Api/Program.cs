using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Youlai.Api.Converters;
using Youlai.Api.WebSockets;
using Youlai.Api.Security;
using Youlai.Api.Middlewares;
using Youlai.Application;
using Youlai.Application.Common.Security;
using Youlai.Application.Common.Results;
using Youlai.Infrastructure;
using Youlai.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services
    .AddControllers()
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
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var jwtSecret = builder.Configuration["Security:Session:Jwt:SecretKey"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.SecurityToken is not System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt)
                    {
                        context.Fail("Invalid token");
                        return Task.CompletedTask;
                    }

                    var tokenManager = context.HttpContext.RequestServices.GetRequiredService<JwtTokenManager>();
                    if (!tokenManager.ValidateAccessToken(jwt.RawData))
                    {
                        context.Fail("Invalid token");
                    }

                    return Task.CompletedTask;
                },
            };
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

// 配置 HTTP 请求管道
app.UseMiddleware<ExceptionHandlingMiddleware>();

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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.Map("/ws", StompWebSocketEndpoint.HandleAsync);

app.MapControllers();

app.Run();

public partial class Program
{
}
