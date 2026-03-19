using System.Text.Json;

namespace PromptEngineering.LLM.Json;

public class JsonNamingPolicyDecorator(JsonNamingPolicy? underlyingNamingPolicy) : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return underlyingNamingPolicy?.ConvertName(name) ?? name;
    }
}