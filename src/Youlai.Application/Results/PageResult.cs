using System.Text.Json.Serialization;

namespace Youlai.Application.Results;

/// <summary>
/// 分页响应体 {code,msg,data:{list,total}}
/// </summary>
public sealed class PageResult<T>
{
    /// <summary>结果码（5 位业务码）</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = ResultCode.Success.Code();

    /// <summary>分页数据负载</summary>
    [JsonPropertyName("data")]
    public PageData<T> Data { get; init; } = new();

    /// <summary>提示信息</summary>
    [JsonPropertyName("msg")]
    public string Msg { get; init; } = ResultCode.Success.Msg();

    /// <summary>分页成功响应</summary>
    public static PageResult<T> Success(IReadOnlyCollection<T> list, int total, int pageNum, int pageSize)
    {
        return new PageResult<T>
        {
            Code = ResultCode.Success.Code(),
            Msg = ResultCode.Success.Msg(),
            Data = new PageData<T>
            {
                List = list ?? Array.Empty<T>(),
                Total = total,
            },
        };
    }

    /// <summary>
    /// 分页数据
    /// </summary>
    public sealed class PageData<TItem>
    {
        /// <summary>当前页数据列表</summary>
        [JsonPropertyName("list")]
        public IReadOnlyCollection<TItem> List { get; init; } = Array.Empty<TItem>();

        /// <summary>符合条件的总记录数</summary>
        [JsonPropertyName("total")]
        public int Total { get; init; }
    }
}
