using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Log;

/// <summary>
/// 鏃ュ織鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
public sealed class LogQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public string[]? CreateTime { get; init; }
}
