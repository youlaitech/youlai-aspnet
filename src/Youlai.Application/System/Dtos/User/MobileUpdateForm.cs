using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 鎵嬫満鍙风粦瀹氭垨淇敼琛ㄥ崟
/// </summary>
public sealed class MobileUpdateForm
{
    [JsonPropertyName("mobile")]
    [Required(ErrorMessage = "鎵嬫満鍙蜂笉鑳戒负绌?)]
    public string? Mobile { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "楠岃瘉鐮佷笉鑳戒负绌?)]
    public string? Code { get; init; }

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "褰撳墠瀵嗙爜涓嶈兘涓虹┖")]
    public string? Password { get; init; }
}
