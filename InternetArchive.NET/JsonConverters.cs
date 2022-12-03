using System.Globalization;

namespace InternetArchive;

public class DateTimeOffsetNullableConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return string.IsNullOrEmpty(value) ? null : DateTimeOffset.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? dateTimeOffset, JsonSerializerOptions options)
    {
        if (dateTimeOffset == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(dateTimeOffset.Value.ToString("O"));
        }
    }
}

public class DateTimeNullableConverter: JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? dateTime, JsonSerializerOptions options)
    {
        if (dateTime == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(dateTime.Value.ToString("O"));
        }
    }
}

public class WaybackDateTimeOffsetNullableConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return string.IsNullOrEmpty(value) ? null : DateTimeOffset.ParseExact(value, Wayback.DateFormat, CultureInfo.InvariantCulture.DateTimeFormat);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? dateTime, JsonSerializerOptions options)
    {
        if (dateTime == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(dateTime.Value.ToString(Wayback.DateFormat));
        }
    }
}

public class UnixEpochDateTimeNullableConverter: JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long? unixTimeSeconds = reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => long.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetInt64(),
            _ => throw new Exception($"Unexpected type {reader.TokenType}")
        };

        return unixTimeSeconds == null ? null : DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds.Value);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? dateTime, JsonSerializerOptions options)
    {
        if (dateTime == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(dateTime.Value.ToUnixTimeSeconds());
        }
    }
}

public class NullableStringToIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<string>();

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (s == null) return null;
            return int.Parse(s);
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        else
        {
            throw new Exception("Unexpected token type");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? i, JsonSerializerOptions options)
    {
        if (i == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(i.ToString());
        }
    }
}

public class EnumerableStringConverter : JsonConverter<IEnumerable<string>>
{
    public override IEnumerable<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<string>();

        if (reader.TokenType == JsonTokenType.Null)
        {
            // ok, ignore
        }
        else if (reader.TokenType == JsonTokenType.String)
        { 
            list.Add(reader.GetString()!);
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                list.Add(reader.GetString()!);
            }
        }
        else
        {
            throw new Exception("Unexpected token type");
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<string> list, JsonSerializerOptions options)
    {
        if (list == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStartArray();
            foreach (var item in list) JsonSerializer.Serialize(writer, item, options);
            writer.WriteEndArray();
        }
    }
}
