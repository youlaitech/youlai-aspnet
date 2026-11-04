using System.Text;

namespace Youlai.Application.Common;

/// <summary>
/// 命名风格转换
/// </summary>
public static class NamingUtils
{
    /// <summary>
    /// 转短横线命名，如 UserName → user-name、user_name → user-name；接口路径、前端目录与菜单路由共用此实现
    /// </summary>
    public static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0 && char.IsUpper(ch))
            {
                sb.Append('-');
            }

            sb.Append(ch == '_' ? '-' : char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 转英文复数短横线命名，如 dict-item → dict-items、category → categories；接口路径与前端目录共用
    /// </summary>
    public static string ToPluralKebab(string value)
    {
        var word = ToKebabCase(value);
        if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("z") || word.EndsWith("ch") || word.EndsWith("sh"))
        {
            return word + "es";
        }

        if (word.Length > 1 && word.EndsWith("y") && "aeiou".IndexOf(word[word.Length - 2]) < 0)
        {
            return word[..^1] + "ies";
        }

        return word + "s";
    }
}
