namespace Youlai.Application.Options;

/// <summary>
/// AI 对话配置（对应 appsettings.json 的 Ai 节点）
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>是否启用 AI 辅助能力</summary>
    public bool Enabled { get; init; }

    /// <summary>OpenAI 兼容接口地址（含 /v1）</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>API 密钥</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>对话模型名称</summary>
    public string Model { get; init; } = string.Empty;
}
