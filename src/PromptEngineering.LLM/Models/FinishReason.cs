using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Json;

namespace PromptEngineering.LLM.Models
{
    [JsonConverter(typeof(JsonEnumMemberStringEnumConverter))]
    public enum FinishReason
    {
        /// <summary>
        /// Omitted content due to a flag from our content filters
        /// </summary>
        [EnumMember(Value = "content_filter")]
        ContentFilter,
        /// <summary>
        /// The model decided to call a function
        /// </summary>
        [EnumMember(Value = "function_call")]
        FunctionCall,
        /// <summary>
        /// Incomplete model output due to max_tokens parameter or token limit
        /// </summary>
        [EnumMember(Value = "length")]
        Length,
        /// <summary>
        /// API returned complete message, or a message terminated by one of
        /// the stop sequences provided via the stop parameter
        /// </summary>
        [EnumMember(Value = "stop")]
        Stop
    }
}
