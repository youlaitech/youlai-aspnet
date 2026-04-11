namespace Youlai.Application.Extensions;

/// <summary>
/// 集合扩展方法
/// </summary>
public static class IdCollectionExtensions
{
    /// <summary>
    /// 将逗号分隔的字符串解析为正整数ID集合
    /// </summary>
    public static HashSet<long> ParsePositiveLongIds(string? input)
    {
        var set = new HashSet<long>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return set;
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (long.TryParse(p, out var v) && v > 0)
            {
                set.Add(v);
            }
        }

        return set;
    }
}
