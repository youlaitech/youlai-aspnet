using Microsoft.Extensions.DependencyInjection;
using Youlai.Application.Auth.Interfaces;
using Youlai.Application.System.Interfaces;
using Youlai.Application.Codegen.Interfaces;
using Youlai.Application.File.Interfaces;
using Youlai.Application.Security;
using Youlai.Application.Common.Interfaces;
using Youlai.Application.Services.Auth;
using Youlai.Application.Services.System;
using Youlai.Application.Services.Codegen;
using Youlai.Application.Services.File;
using Youlai.Application.Common.Services;

namespace Youlai.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<IWxMaAuthService, WxMaAuthService>();

        // System services
        services.AddScoped<ISystemUserService, SystemUserService>();
        services.AddScoped<ISystemRoleService, SystemRoleService>();
        services.AddScoped<ISystemMenuService, SystemMenuService>();
        services.AddScoped<ISystemDeptService, SystemDeptService>();
        services.AddScoped<ISystemDictService, SystemDictService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<ISystemNoticeService, SystemNoticeService>();

        // Codegen service
        services.AddScoped<ICodegenService, CodegenService>();

        // File service
        services.AddScoped<IFileService, FileService>();

        // Security services
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IRolePermsCacheInvalidator, RolePermsCacheInvalidator>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();

        // Common services
        services.AddSingleton<ISseService, SseService>();
        services.AddScoped<ILoggingService, LoggingService>();

        return services;
    }
}
