using System.ComponentModel.DataAnnotations;

namespace Youlai.Core.Enums;

/// <summary>
/// 操作类型枚举
/// </summary>
public enum ActionType
{
    /// <summary>登录</summary>
    [Display(Name = "登录")]
    LOGIN = 1,
    /// <summary>登出</summary>
    [Display(Name = "登出")]
    LOGOUT = 2,
    /// <summary>新增</summary>
    [Display(Name = "新增")]
    INSERT = 3,
    /// <summary>修改</summary>
    [Display(Name = "修改")]
    UPDATE = 4,
    /// <summary>删除</summary>
    [Display(Name = "删除")]
    DELETE = 5,
    /// <summary>授权</summary>
    [Display(Name = "授权")]
    GRANT = 6,
    /// <summary>导出</summary>
    [Display(Name = "导出")]
    EXPORT = 7,
    /// <summary>导入</summary>
    [Display(Name = "导入")]
    IMPORT = 8,
    /// <summary>上传</summary>
    [Display(Name = "上传")]
    UPLOAD = 9,
    /// <summary>下载</summary>
    [Display(Name = "下载")]
    DOWNLOAD = 10,
    /// <summary>修改密码</summary>
    [Display(Name = "修改密码")]
    CHANGE_PASSWORD = 11,
    /// <summary>重置密码</summary>
    [Display(Name = "重置密码")]
    RESET_PASSWORD = 12,
    /// <summary>启用</summary>
    [Display(Name = "启用")]
    ENABLE = 13,
    /// <summary>禁用</summary>
    [Display(Name = "禁用")]
    DISABLE = 14,
    /// <summary>查询列表</summary>
    [Display(Name = "查询列表")]
    LIST = 15,
    /// <summary>其他</summary>
    [Display(Name = "其他")]
    OTHER = 99,
}
