using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Results;

/// <summary>
/// 统一响应体 {code,msg,data}
/// </summary>
public sealed class Result<T>
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = ResultCode.Success.Code();

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("msg")]
    public string Msg { get; init; } = ResultCode.Success.Msg();

    public static Result<T> Success(T? data = default)
    {
        return new Result<T>
        {
            Code = ResultCode.Success.Code(),
            Msg = ResultCode.Success.Msg(),
            Data = data,
        };
    }

    public static Result<T> Failed(ResultCode code, string? msg = null, T? data = default)
    {
        return new Result<T>
        {
            Code = code.Code(),
            Msg = string.IsNullOrWhiteSpace(msg) ? code.Msg() : msg,
            Data = data,
        };
    }
}

/// <summary>
/// Result 工具方法
/// </summary>
public static class Result
{
    public static Result<object?> Success() => Result<object?>.Success();

    public static Result<T> Success<T>(T? data) => Result<T>.Success(data);

    public static Result<object?> Failed(ResultCode code, string? msg = null) => Result<object?>.Failed(code, msg);

    public static Result<object?> Judge(bool ok) => ok ? Success() : Failed(ResultCode.SystemError);
}
