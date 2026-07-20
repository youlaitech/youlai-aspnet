using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youlai.Application.Converters;

/// <summary>
/// 字符串数组转换器：JSON 数组中的元素既可为数字也可为字符串，统一按字符串读出。
/// 数字元素按不变文化格式化为字符串（如 1 → "1"），小数退化为字符串形式保留。
/// </summary>
public sealed class StringListFlexibleConverter : JsonConverter<IReadOnlyCollection<string>?>
{
    public override IReadOnlyCollection<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected start of array, got {reader.TokenType}");
        }

        var list = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var s = reader.GetString();
                    if (s is not null)
                    {
                        list.Add(s);
                    }
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l))
                    {
                        list.Add(l.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        list.Add(reader.GetDouble().ToString(CultureInfo.InvariantCulture));
                    }
                    break;
                case JsonTokenType.Null:
                    break;
                default:
                    throw new JsonException($"Unexpected token in string array: {reader.TokenType}");
            }
        }

        throw new JsonException("Unexpected end of array while reading string list");
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<string>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}
