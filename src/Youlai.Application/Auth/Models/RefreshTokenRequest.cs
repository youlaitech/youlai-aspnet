using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Auth.Models;

/// <summary>
/// 刷新令牌请求
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>刷新令牌</summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    [StringLength(4096, ErrorMessage = "刷新令牌长度不合法")]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
}
