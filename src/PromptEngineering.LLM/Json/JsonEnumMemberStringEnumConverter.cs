using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Json;

public class JsonEnumMemberStringEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
    : JsonConverterFactory
{
    private readonly JsonStringEnumConverter _baseConverter = new(namingPolicy, allowIntegerValues);

    public JsonEnumMemberStringEnumConverter()
        : this(null)
    {
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return _baseConverter.CanConvert(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var query = from field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static)
            let attr = field.GetCustomAttribute<EnumMemberAttribute>()
            where attr != null && attr.Value != null
            select (field.Name, attr.Value);
        var dictionary = query.ToDictionary(p => p.Item1, p => p.Item2);
        if (dictionary.Count > 0)
            return new JsonStringEnumConverter(new DictionaryLookupNamingPolicy(dictionary, namingPolicy),
                allowIntegerValues).CreateConverter(typeToConvert, options);

        return _baseConverter.CreateConverter(typeToConvert, options);
    }
}