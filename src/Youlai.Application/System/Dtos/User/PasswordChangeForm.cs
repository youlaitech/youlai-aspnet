using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 淇敼瀵嗙爜琛ㄥ崟
/// </summary>
public sealed class PasswordChangeForm
{
    [JsonPropertyName("oldPassword")]
    [Required(ErrorMessage = "鍘熷瘑鐮佷笉鑳戒负绌?)]
    public string? OldPassword { get; init; }

    [JsonPropertyName("newPassword")]
    [Required(ErrorMessage = "鏂板瘑鐮佷笉鑳戒负绌?)]
    public string? NewPassword { get; init; }

    [JsonPropertyName("confirmPassword")]
    [Required(ErrorMessage = "纭瀵嗙爜涓嶈兘涓虹┖")]
    public string? ConfirmPassword { get; init; }
}
