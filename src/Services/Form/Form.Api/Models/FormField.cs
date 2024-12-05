using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Form.Api.Models;

public class FormField
{
    [Required]
    public Guid Id { get; set; }
    
    public FieldType Type { get; set; }
    
    public Properties Properties { get; set; }
}

public class Properties
{
    [JsonPropertyName("text_input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextInputDetails? TextInput { get; set; }
    
    [JsonPropertyName("select")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SelectDetails? Select { get; set; }
}

public class TextInputDetails
{
    public string Placeholder { get; set; }
}
public class SelectDetails
{
    public string Placeholder { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<FieldType>))]
public enum FieldType
{
    text_input,
    select
}