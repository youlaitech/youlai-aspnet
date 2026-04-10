using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Youlai.Application.Common.Interfaces;
using Youlai.Application.Security;

namespace Youlai.Api.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Security:Session:Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            return services;
        }

        services
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

        return services;
    }
}
