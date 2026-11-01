using System.ComponentModel.DataAnnotations;

namespace Youlai.Application.Realtime;

/// <summary>
/// 申请握手票据时提交的客户端实例、平台与订阅主题
/// </summary>
public sealed class CreateRealtimeTicketRequest
{
    /// <summary>客户端实例标识，同一实例重连时复用</summary>
    [Required]
    [RegularExpression("[A-Za-z0-9_-]{16,128}", ErrorMessage = "clientInstanceId 长度须为 16-128，且仅含字母、数字、下划线与短横线")]
    public string ClientInstanceId { get; set; } = string.Empty;

    /// <summary>运行平台，取 web、mp-weixin 或 app</summary>
    [Required]
    [RegularExpression("web|mp-weixin|app", ErrorMessage = "platform 仅支持 web、mp-weixin、app")]
    public string Platform { get; set; } = string.Empty;

    /// <summary>订阅主题，须包含 notice，且仅支持 notice、dict、presence</summary>
    [Required]
    [MinLength(1, ErrorMessage = "topics 不能为空")]
    public List<string> Topics { get; set; } = new();
}
