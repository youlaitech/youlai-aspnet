using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Results;

/// <summary>
/// 分页响应体 {code,msg,data:{list,total}}
/// </summary>
public sealed class PageResult<T>
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = ResultCode.Success.Code();

    [JsonPropertyName("data")]
    public IReadOnlyCollection<T> Data { get; init; } = Array.Empty<T>();

    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PageMeta? Page { get; init; }

    [JsonPropertyName("msg")]
    public string Msg { get; init; } = ResultCode.Success.Msg();

    public static PageResult<T> Success(IReadOnlyCollection<T> list, long total, int pageNum, int pageSize)
    {
        return new PageResult<T>
        {
            Code = ResultCode.Success.Code(),
            Msg = ResultCode.Success.Msg(),
            Data = list ?? Array.Empty<T>(),
            Page = new PageMeta
            {
                PageNum = pageNum,
                PageSize = pageSize,
                Total = total,
            },
        };
    }

    /// <summary>
    /// 分页数据
    /// </summary>
    public sealed class PageMeta
    {
        [JsonPropertyName("pageNum")]
        public int PageNum { get; init; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; init; }

        [JsonPropertyName("total")]
        public long Total { get; init; }
    }
}
