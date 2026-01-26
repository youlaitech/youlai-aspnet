using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 密码校验表单
/// </summary>
public sealed class PasswordVerifyForm
{
    /// <summary>
    /// 当前密码
    /// </summary>
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "当前密码不能为空")]
    public string? Password { get; init; }
}
