using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Auth.Models;

/// <summary>
/// 微信小程序绑定手机号请求
/// </summary>
public sealed class WxMaBindMobileForm
{
    /// <summary>提交绑定前重新获取的 wx.login code，服务端据此确认 openid</summary>
    [Required(ErrorMessage = "微信登录凭证不能为空")]
    [JsonPropertyName("loginCode")]
    public string LoginCode { get; init; } = string.Empty;

    /// <summary>手机号码</summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [JsonPropertyName("mobile")]
    public string Mobile { get; init; } = string.Empty;

    /// <summary>短信验证码</summary>
    [Required(ErrorMessage = "短信验证码不能为空")]
    [JsonPropertyName("smsCode")]
    public string SmsCode { get; init; } = string.Empty;
}
