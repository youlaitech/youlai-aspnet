using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 手机号绑定或修改表单
/// </summary>
public sealed class MobileUpdateForm
{
    [JsonPropertyName("mobile")]
    [Required(ErrorMessage = "手机号不能为空")]
    public string? Mobile { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "验证码不能为空")]
    public string? Code { get; init; }

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "当前密码不能为空")]
    public string? Password { get; init; }
}
