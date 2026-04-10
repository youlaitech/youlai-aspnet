using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 修改密码表单
/// </summary>
public sealed class PasswordChangeForm
{
    /// <summary>
    /// 原密码
    /// </summary>
    [JsonPropertyName("oldPassword")]
    [Required(ErrorMessage = "原密码不能为空")]
    public string? OldPassword { get; init; }

    /// <summary>
    /// 新密码
    /// </summary>
    [JsonPropertyName("newPassword")]
    [Required(ErrorMessage = "新密码不能为空")]
    public string? NewPassword { get; init; }

    /// <summary>
    /// 确认新密码
    /// </summary>
    [JsonPropertyName("confirmPassword")]
    [Required(ErrorMessage = "确认密码不能为空")]
    public string? ConfirmPassword { get; init; }
}
