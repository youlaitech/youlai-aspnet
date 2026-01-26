using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 配置分页查询参数
/// </summary>
public sealed class ConfigQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }
}
