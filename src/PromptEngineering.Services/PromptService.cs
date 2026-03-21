using Microsoft.Extensions.Options;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;
using PromptEngineering.Model;

namespace PromptEngineering.Services;

public sealed class PromptService : IPromptService
{
    private readonly IContextService _contextService;
    private readonly ContextSettings _contextSettings;

    public PromptService(IContextService contextService, IOptions<ContextSettings> contextSettings)
    {
        ArgumentNullException.ThrowIfNull(contextService);
        ArgumentNullException.ThrowIfNull(contextSettings);
        _contextService = contextService;
        _contextSettings = contextSettings.Value;
    }

    public async Task<ChatRequest> BuildAsync(CancellationToken cancellationToken = default)
    {
        await _contextService.UpsertAsync(
            new Context(_contextSettings.AssistantRoleKey, _contextSettings.DefaultAssistantRole),
            cancellationToken);

        var assistantRole = await _contextService.GetByKeyAsync(_contextSettings.AssistantRoleKey, cancellationToken);

        var chatRequest = new ChatRequest
        {
            Temperature = _contextSettings.Temperature
        };
        chatRequest.AddSystemMessage(assistantRole?.Value ?? _contextSettings.DefaultAssistantRole);
        chatRequest.AddUserMessage(_contextSettings.DefaultUserPrompt);

        return chatRequest;
    }
}
