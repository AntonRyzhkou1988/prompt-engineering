using PromptEngineering.LLM.Models;

namespace PromptEngineering.Services;

public interface IPromptService
{
    Task<ChatRequest> BuildAsync(CancellationToken cancellationToken = default);
}
