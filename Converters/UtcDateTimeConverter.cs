using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace EMS.Api.Converters;

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    // ISO 8601 round-trip format specifier
    private const string Format = "o";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the string and convert it to a UTC DateTime
        return reader.GetDateTime().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // If the DateTime kind is Unspecified (like from a database without timezone info),
        // assume it was intended to be UTC.
        DateTime utcValue = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        
        // Write the value in the standard ISO 8601 format (e.g., "2025-10-07T09:30:00.123Z")
        writer.WriteStringValue(utcValue.ToString(Format));
    }
}