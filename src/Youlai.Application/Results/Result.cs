using System.Text.Json.Serialization;

namespace Youlai.Application.Results;

/// <summary>
/// 统一响应体契约（非泛型视图，供操作日志等基础设施读取业务状态，无需感知泛型参数）
/// </summary>
public interface IResult
{
    /// <summary>结果码（5 位业务码）</summary>
    string Code { get; }

    /// <summary>提示信息</summary>
    string Msg { get; }
}

/// <summary>
/// 统一响应体 {code,msg,data}
/// </summary>
public sealed class Result<T> : IResult
{
    /// <summary>结果码（5 位业务码）</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = ResultCode.Success.Code();

    /// <summary>业务数据负载</summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>提示信息</summary>
    [JsonPropertyName("msg")]
    public string Msg { get; init; } = ResultCode.Success.Msg();

    /// <summary>成功响应，携带业务数据</summary>
    public static Result<T> Success(T? data = default)
    {
        return new Result<T>
        {
            Code = ResultCode.Success.Code(),
            Msg = ResultCode.Success.Msg(),
            Data = data,
        };
    }

    /// <summary>失败响应，msg 为空时取 code 对应默认文案</summary>
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
