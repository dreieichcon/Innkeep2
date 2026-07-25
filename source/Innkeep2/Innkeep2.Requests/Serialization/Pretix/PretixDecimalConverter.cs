using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Innkeep2.Requests.Serialization.Pretix;

public sealed class PretixDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.String => reader.GetString(),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for decimal value.")
        };

        return Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));

    public static decimal Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("Empty value where a decimal was expected.");

        // Strip anything that isn't a digit, '.', ',' or '-'
        var cleaned = Regex.Replace(raw, @"[^\d,.\-]", "");

        var lastComma = cleaned.LastIndexOf(',');
        var lastDot = cleaned.LastIndexOf('.');

        // Whichever separator appears last is the decimal separator;
        // any earlier occurrences of either are thousands separators and get stripped.
        char? decimalSeparator = (lastComma, lastDot) switch
        {
            ( -1, -1) => null,
            (var c, var d) when c > d => ',',
            _ => '.'
        };

        string normalized;
        if (decimalSeparator is null)
        {
            normalized = cleaned;
        }
        else
        {
            var separatorIndex = cleaned.LastIndexOf(decimalSeparator.Value);
            var integerPart = cleaned[..separatorIndex].Replace(",", "").Replace(".", "");
            var fractionalPart = cleaned[(separatorIndex + 1)..];
            normalized = $"{integerPart}.{fractionalPart}";
        }

        return decimal.Parse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}