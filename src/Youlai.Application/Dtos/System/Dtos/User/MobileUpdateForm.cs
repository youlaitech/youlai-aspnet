using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 手机号绑定或修改表单
/// </summary>
public sealed class MobileUpdateForm
{
    /// <summary>
    /// 手机号
    /// </summary>
    [JsonPropertyName("mobile")]
    [Required(ErrorMessage = "手机号不能为空")]
    public string? Mobile { get; init; }

    /// <summary>
    /// 短信验证码
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
