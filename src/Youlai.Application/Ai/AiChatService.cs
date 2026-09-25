using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Youlai.Application.Exceptions;
using Youlai.Application.Options;
using Youlai.Application.Results;

namespace Youlai.Application.Ai;

/// <summary>
/// AI 对话服务实现：未启用 AI 时调用直接抛业务异常
/// </summary>
internal sealed class AiChatService : IAiChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(IHttpClientFactory httpClientFactory, IOptions<AiOptions> options, ILogger<AiChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var content = await CompleteAsync(systemPrompt, userPrompt, jsonMode: false, cancellationToken).ConfigureAwait(false);
        return content.Trim();
    }

    /// <inheritdoc />
    public async Task<T?> ChatEntityAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default) where T : class
    {
        var content = await CompleteAsync(systemPrompt, userPrompt, jsonMode: true, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(TrimCodeFence(content), JsonOptions);
    }

    /// <inheritdoc />
    public async Task<List<Dictionary<string, object?>>?> ChatJsonListAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var content = await CompleteAsync(systemPrompt, userPrompt, jsonMode: true, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(TrimCodeFence(content), JsonOptions);
    }

    /// <summary>
    /// 调用 /chat/completions 并返回首条回复正文
    /// </summary>
    private async Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool jsonMode, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "AI 功能未开启，请配置 Ai.Enabled 与 ApiKey");
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };
        if (jsonMode)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        var client = _httpClientFactory.CreateClient("Ai");
        client.BaseAddress ??= new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        _logger.LogInformation("AI 调用开始");
        var started = DateTime.UtcNow;
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("AI 调用失败 {Status}", response.StatusCode);
            throw new BusinessException(ResultCode.InvalidUserInput, "AI 调用失败：" + (int)response.StatusCode);
        }

        using var document = JsonDocument.Parse(body);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        _logger.LogInformation("AI 调用完成，耗时 {Elapsed} ms", (DateTime.UtcNow - started).TotalMilliseconds);
        return content;
    }

    /// <summary>
    /// 去掉模型输出里可能包裹的 json 代码围栏
    /// </summary>
    private static string TrimCodeFence(string content)
    {
        var text = content.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstLineEnd = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? text[(firstLineEnd + 1)..lastFence].Trim()
            : text;
    }
}
