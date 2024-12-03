using System.Text.Json;
using System.Text.Json.Serialization;

namespace Form.Api.Models;

public class FormField
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(FieldTypeJsonConverter))]
    public FieldType Type { get; set; }
    
    //[JsonPropertyName("properties")]
    //public Properties Properties { get; set; }
}

public class FieldTypeJsonConverter : JsonConverter<FieldType>
{
    public override FieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            //throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            //throw new JsonException();
        }

        var propertyName = reader.GetString();
        if (propertyName != "type")
        {
            //throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
        {
            //throw new JsonException();
        }

        var value = reader.GetString();
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndObject)
        {
            //throw new JsonException();
        }

        return value switch
        {
            "text_input" => FieldType.TextInput,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, FieldType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.ToString());
        writer.WriteEndObject();
    }
}

public class Properties
{
    [JsonPropertyName("text_input")]
    public TextInputDetails? TextInput { get; set; }
}

public class TextInputDetails
{
    public string Placeholder { get; set; }
}

public enum FieldType
{
    TextInput
}