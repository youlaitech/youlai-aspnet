namespace Youlai.Infrastructure.Options;

/// <summary>
/// AI 配置
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string BaseUrl { get; init; } = "https://dashscope.aliyuncs.com/compatible-mode";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "qwen-plus";

    public int TimeoutMs { get; init; } = 20000;

    public string Provider { get; init; } = "qwen";
}
