using System.Text.Json.Serialization;

namespace MockServer.Domain.Shared
{
    public class TemplateRenderedValue {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TemplateEngine Engine { get; set; }
        public string Template { get; set; }
        public string Format { get; set; }
    }
}