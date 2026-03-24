using System.ComponentModel.DataAnnotations.Schema;

namespace Youlai.Domain.Entities;

/// <summary>
/// 系统操作日志实体
/// </summary>
public sealed class SysLog
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 模块
    /// </summary>
    public int? Module { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Column("action_type")]
    public int? ActionType { get; set; }

    /// <summary>
    /// 操作标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 自定义日志内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 操作人ID
    /// </summary>
    [Column("operator_id")]
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作人名称
    /// </summary>
    [Column("operator_name")]
    public string? OperatorName { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [Column("request_uri")]
    public string? RequestUri { get; set; }

    /// <summary>
    /// 请求方式
    /// </summary>
    [Column("request_method")]
    public string? RequestMethod { get; set; }

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
    /// 设备
    /// </summary>
    public string? Device { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 状态：0失败 1成功
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [Column("error_msg")]
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// 执行耗时(ms)
    /// </summary>
    [Column("execution_time")]
    public int? ExecutionTime { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [Column("create_time")]
    public DateTime? CreateTime { get; set; }
}
