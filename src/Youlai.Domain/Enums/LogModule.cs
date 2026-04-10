using System.ComponentModel.DataAnnotations;

namespace Youlai.Domain.Enums;

/// <summary>
/// 日志模块枚举
/// </summary>
public enum LogModule
{
    /// <summary>登录</summary>
    [Display(Name = "登录")]
    LOGIN = 1,
    /// <summary>用户管理</summary>
    [Display(Name = "用户管理")]
    USER = 2,
    /// <summary>角色管理</summary>
    [Display(Name = "角色管理")]
    ROLE = 3,
    /// <summary>部门管理</summary>
    [Display(Name = "部门管理")]
    DEPT = 4,
    /// <summary>菜单管理</summary>
    [Display(Name = "菜单管理")]
    MENU = 5,
    /// <summary>字典管理</summary>
    [Display(Name = "字典管理")]
    DICT = 6,
    /// <summary>系统配置</summary>
    [Display(Name = "系统配置")]
    CONFIG = 7,
    /// <summary>文件管理</summary>
    [Display(Name = "文件管理")]
    FILE = 8,
    /// <summary>通知公告</summary>
    [Display(Name = "通知公告")]
    NOTICE = 9,
    /// <summary>日志管理</summary>
    [Display(Name = "日志管理")]
    LOG = 10,
    /// <summary>代码生成</summary>
    [Display(Name = "代码生成")]
    CODEGEN = 11,
    /// <summary>其他</summary>
    [Display(Name = "其他")]
    OTHER = 99,
}
