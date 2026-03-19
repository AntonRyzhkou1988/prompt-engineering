using System.Text.Json;
using PromptEngineering.LLM.Extensions;

namespace PromptEngineering.LLM.Json;

internal class DictionaryLookupNamingPolicy(
    Dictionary<string, string> dictionary,
    JsonNamingPolicy? underlyingNamingPolicy)
    : JsonNamingPolicyDecorator(underlyingNamingPolicy)
{
    private readonly Dictionary<string, string> _dictionary = dictionary ?? throw new ArgumentNullException();

    public override string ConvertName(string name)
    {
        return _dictionary.TryGetValue(name, out var value) ? value : base.ConvertName(name);
    }
}