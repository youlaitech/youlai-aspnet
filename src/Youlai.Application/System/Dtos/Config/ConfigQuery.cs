using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 閰嶇疆鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
public sealed class ConfigQuery : BaseQuery
{
    public string? Keywords { get; init; }
}
