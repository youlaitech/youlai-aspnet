using System.ComponentModel.DataAnnotations.Schema;

namespace Youlai.Domain.Entities;

public sealed class SysLog
{
    public long Id { get; set; }

    public int? Module { get; set; }

    [Column("action_type")]
    public int? ActionType { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    [Column("operator_id")]
    public long? OperatorId { get; set; }

    [Column("operator_name")]
    public string? OperatorName { get; set; }

    [Column("request_uri")]
    public string? RequestUri { get; set; }

    [Column("request_method")]
    public string? RequestMethod { get; set; }

    public string? Ip { get; set; }

    public string? Province { get; set; }

    public string? City { get; set; }

    public string? Device { get; set; }

    public string? Os { get; set; }

    public string? Browser { get; set; }

    /// <summary>
    /// 状态：0失败 1成功
    /// </summary>
    public int Status { get; set; }

    [Column("error_msg")]
    public string? ErrorMsg { get; set; }

    [Column("execution_time")]
    public int? ExecutionTime { get; set; }

    [Column("create_time")]
    public DateTime? CreateTime { get; set; }
}
