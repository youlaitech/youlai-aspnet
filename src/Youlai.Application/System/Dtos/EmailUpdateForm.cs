using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 邮箱绑定或修改表单
/// </summary>
public sealed class EmailUpdateForm
{
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "邮箱不能为空")]
    public string? Email { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "验证码不能为空")]
    public string? Code { get; init; }
}
