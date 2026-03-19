using System.Text.Json;

namespace PromptEngineering.LLM.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var desc = JsonSerializer.Serialize(value, value.GetType());
        return desc.Replace("\"", "");
    }
}
