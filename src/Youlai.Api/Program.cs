using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "youlai-aspnet",
        Description = "youlai 全家桶（ASP.NET Core 8）权限管理后台接口文档",
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

var jwtSecret = builder.Configuration["Security:Session:Jwt:SecretKey"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.IncludeErrorDetails = true;
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
                OnMessageReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (string.IsNullOrWhiteSpace(authHeader))
                    {
                        logger.LogWarning("JWT message received with empty Authorization header.");
                    }
                    else
                    {
                        logger.LogWarning(
                            "JWT message received. HeaderLength={Length}, StartsWithBearer={StartsWithBearer}",
                            authHeader.Length,
                            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

                        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var rawToken = authHeader["Bearer ".Length..].Trim();
                            try
                            {
                                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                                var jwt = handler.ReadJwtToken(rawToken);
                                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var exp = jwt.Payload.Expiration ?? 0;
                                var iat = new DateTimeOffset(jwt.Payload.IssuedAt).ToUnixTimeSeconds();
                                var sub = jwt.Subject ?? string.Empty;
                                var tokenType = jwt.Payload.TryGetValue("tokenType", out var tt) ? tt?.ToString() : null;
                                var securityVersion = jwt.Payload.TryGetValue("securityVersion", out var sv) ? sv?.ToString() : null;

                                logger.LogWarning(
                                    "JWT payload info: Alg={Alg}, Exp={Exp}, Iat={Iat}, Now={Now}, Sub={Sub}, Jti={Jti}, TokenType={TokenType}, SecurityVersion={SecurityVersion}",
                                    jwt.Header.Alg,
                                    exp,
                                    iat,
                                    now,
                                    sub,
                                    jwt.Id,
                                    tokenType,
                                    securityVersion);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "JWT message received but failed to read token payload.");
                            }
                        }
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    string? rawToken = null;
                    string? jti = null;

                    if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt)
                    {
                        rawToken = jwt.RawData;
                        jti = jwt.Id;
                    }
                    else if (context.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken)
                    {
                        rawToken = jsonWebToken.EncodedToken;
                        jti = jsonWebToken.Id;
                    }
                    else
                    {
                        logger.LogWarning(
                            "JWT validation failed in OnTokenValidated: unsupported token type {TokenType}.",
                            context.SecurityToken?.GetType().FullName ?? "null");
                        context.Fail("Invalid token");
                        return Task.CompletedTask;
                    }

                    var tokenManager = context.HttpContext.RequestServices.GetRequiredService<JwtTokenManager>();
                    if (string.IsNullOrWhiteSpace(rawToken) || !tokenManager.ValidateAccessToken(rawToken))
                    {
                        logger.LogWarning("JWT validation failed in OnTokenValidated. JTI={Jti}", jti ?? string.Empty);
                        context.Fail("Invalid token");
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(context.Exception, "JWT authentication failed.");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(
                        "JWT challenge triggered. Error={Error}, ErrorDescription={ErrorDescription}",
                        context.Error,
                        context.ErrorDescription);
                    if (context.AuthenticateFailure != null)
                    {
                        logger.LogWarning(
                            context.AuthenticateFailure,
                            "JWT authenticate failure captured in challenge.");
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

app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.Map("/ws", StompWebSocketEndpoint.HandleAsync);

app.MapControllers();

app.Run();

public partial class Program
{
}
