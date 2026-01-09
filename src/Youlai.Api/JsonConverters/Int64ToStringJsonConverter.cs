using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youlai.Api.JsonConverters;

/// <summary>
/// 将 int64 序列化为字符串，避免 JavaScript 精度丢失
/// </summary>
/// <remarks>
/// 用于全局 JSON 序列化配置，将 long 输出为字符串
/// </remarks>
public sealed class Int64ToStringJsonConverter : JsonConverter<long>
{
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

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// 将可空 int64 序列化为字符串，避免 JavaScript 精度丢失
/// </summary>
/// <remarks>
/// 用于全局 JSON 序列化配置，将 long? 输出为字符串
/// </remarks>
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
