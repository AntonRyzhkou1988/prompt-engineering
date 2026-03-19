using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models
{
    public record ResponseFormat
    {
        /// <summary>
        /// Response format type.
        /// <remarks>using to set completion format type</remarks>
        /// </summary>
        [JsonPropertyName("type")]
        public string Value { get; set; } = null!;

        /// <summary>
        /// JSON schema of the response.
        /// <remarks>Applicable only when <see cref="Value"/> is set to "json_schema"</remarks>
        /// </summary>
        [JsonPropertyName("json_schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? Schema { get; set; }
    }
}
