using System.Text.Json;

namespace PromptEngineering.LLM.Models;

/// <summary>
/// Configuration for JSON Schema response format.
/// This class is used for configuration binding (e.g., from appsettings.json or external configuration sources).
/// </summary>
public class JsonSchemaSettings
{
    /// <summary>
    /// Gets or sets the description of the JSON schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the JSON schema.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema definition as a JSON string.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets whether to use strict schema validation.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Converts the Schema JSON string to an object structure.
    /// </summary>
    /// <returns>The parsed schema object, or null if Schema is null or empty.</returns>
    public object? GetSchemaObject()
    {
        if (string.IsNullOrWhiteSpace(Schema))
            return null;

        using var jsonDocument = JsonDocument.Parse(Schema);
        
        return ConvertJsonElementToObject(jsonDocument.RootElement);
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(prop => prop.Name, prop => ConvertJsonElementToObject(prop.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElementToObject)
                .ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}

