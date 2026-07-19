using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youlai.Api.Converters;

/// <summary>
/// long → string 转换器，避免 JavaScript 大数精度丢失
/// </summary>
public sealed class Int64ToStringJsonConverter : JsonConverter<long>
{
    /// <summary>
    /// 从 JSON 读取 long：接受字符串或数字，非法抛 JsonException
    /// </summary>
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Invalid int64 string value: {s}");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"Unexpected token parsing int64. Token: {reader.TokenType}");
    }

    /// <summary>
    /// 把 long 写成 JSON 字符串，避免 JavaScript 大数精度丢失
    /// </summary>
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// long? → string 转换器，避免 JavaScript 大数精度丢失
/// </summary>
public sealed class NullableInt64ToStringJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Invalid int64 string value: {s}");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"Unexpected token parsing nullable int64. Token: {reader.TokenType}");
    }

    /// <summary>
    /// 把 long? 写成 JSON 字符串或 null
    /// </summary>
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
