using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youlai.Api.Converters;

/// <summary>
/// DateTime → string 转换器，统一 yyyy-MM-dd HH:mm:ss（DateTime? 由 System.Text.Json 自动套用本转换器）
/// </summary>
public sealed class DateTimeToStringJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// 从 JSON 读取 DateTime：优先按统一格式解析，兼容 ISO 8601 等其它写法，非法抛 JsonException
    /// </summary>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return value;
        }

        throw new JsonException($"Invalid datetime value: {text}");
    }

    /// <summary>
    /// 把 DateTime 写成 yyyy-MM-dd HH:mm:ss 字符串
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
