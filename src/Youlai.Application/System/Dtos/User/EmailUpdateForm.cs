using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 閭缁戝畾鎴栦慨鏀硅〃鍗?
/// </summary>
public sealed class EmailUpdateForm
{
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "閭涓嶈兘涓虹┖")]
    public string? Email { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "楠岃瘉鐮佷笉鑳戒负绌?)]
    public string? Code { get; init; }

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "褰撳墠瀵嗙爜涓嶈兘涓虹┖")]
    public string? Password { get; init; }
}
