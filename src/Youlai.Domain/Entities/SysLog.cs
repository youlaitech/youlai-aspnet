namespace Youlai.Domain.Entities;

/// <summary>
/// 系统操作日志实体
/// </summary>
/// <remarks>
/// 对应系统操作日志数据
/// </remarks>
public sealed class SysLog
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 模块
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 请求方式
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? RequestParams { get; set; }

    /// <summary>
    /// 响应内容
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// 操作内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 请求地址
    /// </summary>
    public string? RequestUri { get; set; }

    /// <summary>
    /// 方法签名
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// 执行耗时(ms)
    /// </summary>
    public long? ExecutionTime { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 浏览器版本
    /// </summary>
    public string? BrowserVersion { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 操作人
    /// </summary>
    public long? CreateBy { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
}
