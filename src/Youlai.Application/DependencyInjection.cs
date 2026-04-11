using Microsoft.Extensions.DependencyInjection;
using Youlai.Application.Auth;
using Youlai.Application.System;
using Youlai.Application.Codegen;
using Youlai.Application.File;
using Youlai.Application.Security;
using Youlai.Application.Common;

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
