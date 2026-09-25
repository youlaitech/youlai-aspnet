using Microsoft.Extensions.DependencyInjection;
using Youlai.Application.Ai;
using Youlai.Application.Auth;
using Youlai.Application.Form;
using Youlai.Application.System;
using Youlai.Application.Codegen;
using Youlai.Application.File;
using Youlai.Application.Security;
using Youlai.Application.Sse;

namespace Youlai.Application;

/// <summary>
/// 应用层服务注册（MediatR、AutoMapper、领域服务等）
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册应用层所有服务（认证、系统管理、代码生成、文件、安全、通用）
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 认证
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWxMaAuthService, WxMaAuthService>();

        // 系统管理相关
        services.AddScoped<ISystemUserService, SystemUserService>();
        services.AddScoped<ISystemRoleService, SystemRoleService>();
        services.AddScoped<ISystemMenuService, SystemMenuService>();
        services.AddScoped<ISystemDeptService, SystemDeptService>();
        services.AddScoped<ISystemDictService, SystemDictService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<ISystemNoticeService, SystemNoticeService>();

        // 动态表单
        services.AddScoped<IFormDefinitionService, FormDefinitionService>();
        services.AddScoped<IFormDataService, FormDataService>();

        // 代码生成
        services.AddScoped<ICodegenService, CodegenService>();

        // 文件
        services.AddScoped<IFileService, FileService>();

        // 安全相关
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IRolePermsCacheInvalidator, RolePermsCacheInvalidator>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();

        // 通用服务
        services.AddSingleton<ISseService, SseService>();

        // AI 辅助
        services.AddSingleton<IPromptLoader, PromptLoader>();
        services.AddScoped<IAiChatService, AiChatService>();

        return services;
    }
}
