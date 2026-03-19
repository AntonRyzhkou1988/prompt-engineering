using PromptEngineering.LLM.Models;

namespace PromptEngineering.LLM.Extensions;

public static class ChatRequestExtensions
{
    public static ChatRequest AddAssistantMessage(this ChatRequest request, string? message)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(message))
        {
            return request;
        }

        request.AddMessage(new ChatMessage
        {
            Role = Role.Assistant,
            Content = message,
            CustomContent = new CustomContent()
        });

        return request;
    }

    public static ChatRequest AddUserMessage(this ChatRequest request, string? message)
    {
        return AddUserMessage(request, message, new CustomContent());
    }

    public static ChatRequest AddUserMessage(this ChatRequest request, string? message, CustomContent? customContent)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(message))
        {
            return request;
        }

        request.AddMessage(new ChatMessage
        {
            Role = Role.User,
            Content = message,
            CustomContent = customContent
        });

        return request;
    }

    public static ChatRequest AddSystemMessage(this ChatRequest request, string? message)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(message))
        {
            return request;
        }

        request.AddMessage(new ChatMessage
        {
            Role = Role.System,
            Content = message,
            CustomContent = new CustomContent()
        });

        return request;
    }

    public static ChatRequest AddMessage(this ChatRequest request, (string Role, string Content) message)
    {
        ArgumentNullException.ThrowIfNull(request);

        return true switch
        {
            _ when message.Role.Equals(Role.Assistant.GetDescription(), StringComparison.OrdinalIgnoreCase) =>
                request.AddAssistantMessage(message.Content),
            _ when message.Role.Equals(Role.User.GetDescription(), StringComparison.OrdinalIgnoreCase) => request
                .AddUserMessage(message.Content),
            _ when message.Role.Equals(Role.System.GetDescription(), StringComparison.OrdinalIgnoreCase) => request
                .AddSystemMessage(message.Content),
            _ => request
        };
    }

    public static ChatRequest AddMessageList(this ChatRequest request,
        IEnumerable<(string Role, string Content)>? messageList)
    {
        ArgumentNullException.ThrowIfNull(request);

        messageList?.ToList().ForEach(i => request.AddMessage(i));

        return request;
    }

    public static ChatRequest SetMaxTokens(this ChatRequest request, int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.MaxTokens = maxTokens;

        return request;
    }

    public static ChatRequest SetTemperature(this ChatRequest request, float temperature)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (temperature < 0 || temperature > 2)
        {
            throw new ArgumentException("The temperature should be between 0 and 2.");
        }
        request.Temperature = temperature;

        return request;
    }

    public static ChatRequest SetResponseFormat(this ChatRequest request, string responseFormat)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(responseFormat))
        {
            throw new ArgumentException("The response format should be not null or empty.");
        }

        request.ResponseFormat = new ResponseFormat() { Value = responseFormat };

        return request;
    }

    public static ChatRequest SetJsonResponseFormat(this ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.ResponseFormat = new ResponseFormat() { Value = "json_object" };
        return request;
    }

    public static ChatRequest SetJsonSchemaResponseFormat(this ChatRequest request, Models.JsonSchema schema)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(schema);

        request.ResponseFormat = new ResponseFormat() { Value = "json_schema", Schema = schema };

        return request;
    }

    /// <summary>
    /// Sets the JSON schema response format using JsonSchemaSettings configuration.
    /// </summary>
    /// <param name="request">The chat request to configure.</param>
    /// <param name="settings">The JSON schema settings to convert and apply.</param>
    /// <returns>The configured chat request.</returns>
    public static ChatRequest SetJsonSchemaResponseFormat(this ChatRequest request, Models.JsonSchemaSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (settings == null)
        {
            return request;
        }

        var jsonSchema = new Models.JsonSchema
        {
            Description = settings.Description,
            Name = settings.Name,
            Schema = settings.GetSchemaObject(),
            Strict = settings.Strict
        };

        return request.SetJsonSchemaResponseFormat(jsonSchema);
    }

    public static ChatRequest SetStreamOption(this ChatRequest request, bool shouldBeStreamed)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Stream = shouldBeStreamed;
        return request;
    }
}
