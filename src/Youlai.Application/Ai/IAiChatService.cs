namespace Youlai.Application.Ai;

/// <summary>
/// AI 对话服务（OpenAI 兼容协议）
/// </summary>
public interface IAiChatService
{
    /// <summary>
    /// 发送提示词并返回模型输出文本
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送提示词并把 JSON 输出反序列化为指定类型
    /// </summary>
    Task<T?> ChatEntityAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送提示词并把 JSON 数组输出反序列化
    /// </summary>
    Task<List<Dictionary<string, object?>>?> ChatJsonListAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
