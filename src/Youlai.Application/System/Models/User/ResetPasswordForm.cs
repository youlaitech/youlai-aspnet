using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.User;

/// <summary>
/// 重置密码表单
/// </summary>
public sealed class ResetPasswordForm
{
    /// <summary>
    /// 新密码
    /// </summary>
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "新密码不能为空")]
    public string? Password { get; init; }
}
