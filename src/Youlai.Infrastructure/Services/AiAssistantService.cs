using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.Platform.Ai.Dtos;
using Youlai.Application.Platform.Ai.Services;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Persistence.DbContext;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services;

internal sealed class AiAssistantService : IAiAssistantService
{
    private const string SystemPrompt = """
        你是一个智能的企业操作助手，需要将用户的自然语言命令解析成标准的函数调用。
        请返回严格的 JSON 格式，包含字段：
        - success: boolean
        - explanation: string
        - confidence: number (0-1)
        - error: string
        - provider: string
        - model: string
        - functionCalls: 数组，每个元素包含 name、description、arguments(对象)
        当无法识别命令时，success=false，并给出 error。
        """;

    private readonly YoulaiDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly AiOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        YoulaiDbContext dbContext,
        ICurrentUser currentUser,
        IChatCompletionService chatCompletionService,
        IOptions<AiOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AiAssistantService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _chatCompletionService = chatCompletionService;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AiParseResponseDto> ParseCommandAsync(AiParseRequestDto request, CancellationToken cancellationToken = default)
    {
        var command = (request.Command ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return new AiParseResponseDto
            {
                Success = false,
                FunctionCalls = Array.Empty<AiFunctionCallDto>(),
                Error = "命令不能为空",
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var (userId, username) = GetCurrentUserInfo();
        var ipAddress = GetIpAddress();

        var record = new AiAssistantRecord
        {
            UserId = userId,
            Username = username,
            OriginalCommand = command,
            AiProvider = _options.Provider,
            AiModel = _options.Model,
            IpAddress = ipAddress,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
        };

        try
        {
            var userPrompt = BuildUserPrompt(command, request);
            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage(userPrompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
            };

            var response = await _chatCompletionService
                .GetChatMessageContentAsync(history, settings, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var rawContent = response?.Content?.ToString() ?? string.Empty;
            var parseResult = ParseAiResponse(rawContent);

            record.AiProvider = string.IsNullOrWhiteSpace(parseResult.Provider) ? _options.Provider : parseResult.Provider;
            record.AiModel = string.IsNullOrWhiteSpace(parseResult.Model) ? _options.Model : parseResult.Model;
            record.ParseStatus = parseResult.Success ? 1 : 0;
            record.FunctionCalls = JsonSerializer.Serialize(parseResult.FunctionCalls);
            record.Explanation = parseResult.Explanation;
            record.Confidence = parseResult.Confidence.HasValue ? Convert.ToDecimal(parseResult.Confidence.Value) : null;
            record.ParseErrorMessage = parseResult.Success ? null : parseResult.Error ?? "无法识别命令";
            record.ParseDurationMs = (int)stopwatch.ElapsedMilliseconds;
            record.UpdateTime = DateTime.Now;

            _dbContext.AiAssistantRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new AiParseResponseDto
            {
                ParseLogId = record.Id,
                Success = parseResult.Success,
                FunctionCalls = parseResult.FunctionCalls,
                Explanation = parseResult.Explanation,
                Confidence = parseResult.Confidence,
                Error = parseResult.Success ? null : record.ParseErrorMessage,
                RawResponse = rawContent,
            };
        }
        catch (Exception ex)
        {
            record.ParseStatus = 0;
            record.FunctionCalls = "[]";
            record.ParseErrorMessage = ex.Message;
            record.ParseDurationMs = (int)stopwatch.ElapsedMilliseconds;
            record.UpdateTime = DateTime.Now;

            _dbContext.AiAssistantRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogError(ex, "AI 命令解析失败");
            return new AiParseResponseDto
            {
                ParseLogId = record.Id,
                Success = false,
                FunctionCalls = Array.Empty<AiFunctionCallDto>(),
                Error = record.ParseErrorMessage ?? "命令解析失败",
            };
        }
    }

    public async Task<AiExecuteResponseDto> ExecuteCommandAsync(AiExecuteRequestDto request, CancellationToken cancellationToken = default)
    {
        var functionCall = request.FunctionCall;
        if (string.IsNullOrWhiteSpace(functionCall?.Name))
        {
            return new AiExecuteResponseDto
            {
                Success = false,
                Error = "functionCall.name is required",
            };
        }

        var (userId, username) = GetCurrentUserInfo();
        var ipAddress = GetIpAddress();

        AiAssistantRecord record;
        if (request.ParseLogId.HasValue)
        {
            record = await _dbContext.AiAssistantRecords
                .FirstOrDefaultAsync(x => x.Id == request.ParseLogId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new BusinessException(ResultCode.InvalidUserInput, $"未找到解析记录: {request.ParseLogId}");
        }
        else
        {
            record = new AiAssistantRecord
            {
                UserId = userId,
                Username = username,
                OriginalCommand = request.OriginalCommand,
                AiProvider = _options.Provider,
                AiModel = _options.Model,
                IpAddress = ipAddress,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
            };
            _dbContext.AiAssistantRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        record.FunctionName = functionCall.Name;
        record.FunctionArguments = JsonSerializer.Serialize(functionCall.Arguments ?? new Dictionary<string, object>());
        record.ExecuteStatus = 0;
        record.UpdateTime = DateTime.Now;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var confirmMode = request.ConfirmMode ?? "auto";
        if (string.Equals(confirmMode, "manual", StringComparison.OrdinalIgnoreCase) && request.UserConfirmed != true)
        {
            return new AiExecuteResponseDto
            {
                Success = false,
                RequiresConfirmation = true,
                ConfirmationPrompt = "需要用户确认后才能执行",
                RecordId = record.Id,
            };
        }

        try
        {
            var result = await ExecuteFunctionCallAsync(functionCall, cancellationToken).ConfigureAwait(false);
            record.ExecuteStatus = 1;
            record.ExecuteErrorMessage = null;
            record.UpdateTime = DateTime.Now;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new AiExecuteResponseDto
            {
                Success = true,
                Data = result,
                Message = "执行成功",
                RecordId = record.Id,
            };
        }
        catch (Exception ex)
        {
            record.ExecuteStatus = -1;
            record.ExecuteErrorMessage = ex.Message;
            record.UpdateTime = DateTime.Now;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogError(ex, "AI 命令执行失败");
            return new AiExecuteResponseDto
            {
                Success = false,
                Error = ex.Message,
                RecordId = record.Id,
            };
        }
    }

    private async Task<object> ExecuteFunctionCallAsync(AiFunctionCallDto functionCall, CancellationToken cancellationToken)
    {
        var functionName = functionCall.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "functionCall.name is required");
        }

        return functionName switch
        {
            "updateUserNickname" => await ExecuteUpdateUserNicknameAsync(functionCall, cancellationToken).ConfigureAwait(false),
            _ => throw new BusinessException(ResultCode.InvalidUserInput, $"不支持的函数: {functionName}"),
        };
    }

    private async Task<object> ExecuteUpdateUserNicknameAsync(AiFunctionCallDto functionCall, CancellationToken cancellationToken)
    {
        var arguments = functionCall.Arguments ?? new Dictionary<string, object>();
        var username = GetArgumentString(arguments, "username");
        var nickname = GetArgumentString(arguments, "nickname");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nickname))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户名或昵称不能为空");
        }

        var user = await _dbContext.SysUsers
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "用户不存在");
        }

        user.Nickname = nickname.Trim();
        if (_currentUser.UserId.HasValue)
        {
            user.UpdateBy = _currentUser.UserId.Value;
        }
        user.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Dictionary<string, object>
        {
            ["username"] = username,
            ["nickname"] = user.Nickname ?? nickname,
            ["message"] = "用户昵称更新成功",
        };
    }

    private static string? GetArgumentString(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.ToString(),
            };
        }

        return value.ToString();
    }

    private string BuildUserPrompt(string command, AiParseRequestDto request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["command"] = command,
            ["currentRoute"] = request.CurrentRoute,
            ["currentComponent"] = request.CurrentComponent,
            ["context"] = request.Context ?? new Dictionary<string, object>(),
            ["availableFunctions"] = AvailableFunctions(),
        };

        return $"请根据以下上下文识别用户意图，并输出符合系统提示要求的 JSON：\n{JsonSerializer.Serialize(payload)}";
    }

    private static IReadOnlyCollection<Dictionary<string, object>> AvailableFunctions()
    {
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["name"] = "updateUserNickname",
                ["description"] = "根据用户名更新用户昵称",
                ["requiredParameters"] = new[] { "username", "nickname" },
            },
        };
    }

    private (long? UserId, string? Username) GetCurrentUserInfo()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var username = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return (_currentUser.UserId, string.IsNullOrWhiteSpace(username) ? null : username);
    }

    private string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return string.Empty;
        }

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var raw = forwarded.Split(',').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                return raw.Trim();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private static ParseResult ParseAiResponse(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new InvalidOperationException("AI 返回内容为空");
        }

        using var document = JsonDocument.Parse(rawContent);
        var root = document.RootElement;

        var success = GetBoolean(root, "success");
        var explanation = GetString(root, "explanation");
        var confidence = GetDouble(root, "confidence");
        var error = GetString(root, "error");
        var provider = GetString(root, "provider");
        var model = GetString(root, "model");

        var calls = new List<AiFunctionCallDto>();
        if (root.TryGetProperty("functionCalls", out var functionCallsElement) && functionCallsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in functionCallsElement.EnumerateArray())
            {
                var name = GetString(item, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var description = GetString(item, "description");
                Dictionary<string, object>? arguments = null;
                if (item.TryGetProperty("arguments", out var argsElement))
                {
                    try
                    {
                        arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText());
                    }
                    catch
                    {
                        arguments = new Dictionary<string, object>();
                    }
                }

                calls.Add(new AiFunctionCallDto
                {
                    Name = name,
                    Description = description,
                    Arguments = arguments ?? new Dictionary<string, object>(),
                });
            }
        }

        return new ParseResult(success, explanation, confidence, error, provider, model, calls);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty(propertyName, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false,
        };
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var strParsed))
        {
            return strParsed;
        }

        return null;
    }

    private sealed record ParseResult(
        bool Success,
        string? Explanation,
        double? Confidence,
        string? Error,
        string? Provider,
        string? Model,
        IReadOnlyCollection<AiFunctionCallDto> FunctionCalls);
}
