using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

public sealed class PasswordVerifyForm
{
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "褰撳墠瀵嗙爜涓嶈兘涓虹┖")]
    public string? Password { get; init; }
}
