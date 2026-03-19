using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Json;

namespace PromptEngineering.LLM.Models
{
    [JsonConverter(typeof(JsonEnumMemberStringEnumConverter))]
    public enum Role
    {
        [EnumMember(Value = "assistant")]
        Assistant,
        [EnumMember(Value = "function")]
        Function,
        [EnumMember(Value = "system")]
        System,
        [EnumMember(Value = "user")]
        User
    }
}
