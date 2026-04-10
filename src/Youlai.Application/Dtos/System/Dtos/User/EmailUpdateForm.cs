using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 邮箱绑定或修改表单
/// </summary>
public sealed class EmailUpdateForm
{
    /// <summary>
    /// 邮箱地址
    /// </summary>
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "邮箱不能为空")]
    public string? Email { get; init; }

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    [JsonPropertyName("code")]
    [Required(ErrorMessage = "验证码不能为空")]
    public string? Code { get; init; }

    /// <summary>
    /// 当前密码
    /// </summary>
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "当前密码不能为空")]
    public string? Password { get; init; }
}
