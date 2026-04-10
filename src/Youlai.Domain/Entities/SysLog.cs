namespace Youlai.Domain.Entities;

public sealed class SysLog
{
    public long Id { get; set; }

    public int? Module { get; set; }

    public int? ActionType { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public long? OperatorId { get; set; }

    public string? OperatorName { get; set; }

    public string? RequestUri { get; set; }

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

    public string? ErrorMsg { get; set; }

    public int? ExecutionTime { get; set; }

    public DateTime? CreateTime { get; set; }
}
