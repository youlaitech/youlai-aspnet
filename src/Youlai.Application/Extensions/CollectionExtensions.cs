namespace Youlai.Application.Extensions;

/// <summary>
/// Collection extension methods
/// </summary>
public static class IdCollectionExtensions
{
    /// <summary>
    /// Parse comma-separated string to HashSet of positive long IDs
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
