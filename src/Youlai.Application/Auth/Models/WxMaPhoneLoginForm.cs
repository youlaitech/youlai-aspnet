using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Auth.Models;

/// <summary>
/// 微信小程序手机号快捷登录请求
/// </summary>
public sealed class WxMaPhoneLoginForm
{
    /// <summary>微信登录凭证（wx.login 获取）</summary>
    [Required(ErrorMessage = "微信登录凭证不能为空")]
    [JsonPropertyName("loginCode")]
    public string LoginCode { get; init; } = string.Empty;

    /// <summary>手机号授权凭证（getPhoneNumber 事件获取）</summary>
    [Required(ErrorMessage = "手机号授权凭证不能为空")]
    [JsonPropertyName("phoneCode")]
    public string PhoneCode { get; init; } = string.Empty;
}
