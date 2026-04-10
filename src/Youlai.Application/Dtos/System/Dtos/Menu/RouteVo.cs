using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 前端路由数据
/// </summary>
public sealed class RouteVo
{
    /// <summary>
    /// 路由路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 组件路径
    /// </summary>
    [JsonPropertyName("component")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Component { get; init; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [JsonPropertyName("redirect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Redirect { get; init; }

    /// <summary>
    /// 路由名称
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// 路由元信息
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RouteMeta? Meta { get; init; }

    /// <summary>
    /// 子路由
    /// </summary>
    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<RouteVo>? Children { get; init; }

    /// <summary>
    /// 路由元信息
    /// </summary>
    public sealed class RouteMeta
    {
        /// <summary>
        /// 标题
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// 图标
        /// </summary>
        [JsonPropertyName("icon")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Icon { get; init; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        [JsonPropertyName("hidden")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Hidden { get; init; }

        /// <summary>
        /// 缓存页面
        /// </summary>
        [JsonPropertyName("keepAlive")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? KeepAlive { get; init; }

        /// <summary>
        /// 始终显示
        /// </summary>
        [JsonPropertyName("alwaysShow")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AlwaysShow { get; init; }

        /// <summary>
        /// 路由参数
        /// </summary>
        [JsonPropertyName("params")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, string>? Params { get; init; }
    }
}
