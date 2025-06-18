using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionCoreBot.Infrastructure.Converters
{

    public class StringToDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => decimal.Parse(reader.GetString(), CultureInfo.InvariantCulture),
                JsonTokenType.Number => reader.GetDecimal(),
                _ => throw new JsonException($"Unexpected token parsing decimal. Token: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }

}
