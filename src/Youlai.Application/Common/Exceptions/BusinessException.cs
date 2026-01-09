using Youlai.Application.Common.Results;

namespace Youlai.Application.Common.Exceptions;

/// <summary>
/// 业务异常
/// </summary>
public sealed class BusinessException : Exception
{
    /// <summary>
    /// 对应的业务状态码
    /// </summary>
    public ResultCode ResultCode { get; }

    public BusinessException(ResultCode resultCode, string? message = null, Exception? innerException = null)
        : base(message ?? resultCode.Msg(), innerException)
    {
        ResultCode = resultCode;
    }
}
