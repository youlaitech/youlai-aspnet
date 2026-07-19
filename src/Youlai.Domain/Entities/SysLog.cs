namespace Youlai.Domain.Entities;

/// <summary>
/// 系统日志实体
/// </summary>
public sealed class SysLog
{
    public long Id { get; set; }

    /// <summary>业务模块</summary>
    public int? Module { get; set; }

    /// <summary>操作类型</summary>
    public int? ActionType { get; set; }

    /// <summary>操作标题</summary>
    public string? Title { get; set; }

    /// <summary>操作内容</summary>
    public string? Content { get; set; }

    /// <summary>操作人 ID</summary>
    public long? OperatorId { get; set; }

    /// <summary>操作人名称</summary>
    public string? OperatorName { get; set; }

    /// <summary>请求地址</summary>
    public string? RequestUri { get; set; }

    /// <summary>请求方法</summary>
    public string? RequestMethod { get; set; }

    /// <summary>客户端 IP</summary>
    public string? Ip { get; set; }

    /// <summary>省份</summary>
    public string? Province { get; set; }

    /// <summary>城市</summary>
    public string? City { get; set; }

    /// <summary>设备类型</summary>
    public string? Device { get; set; }

    /// <summary>操作系统</summary>
    public string? Os { get; set; }

    /// <summary>浏览器</summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 状态：0失败 1成功
    /// </summary>
    public int Status { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMsg { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public int? ExecutionTime { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }
}
