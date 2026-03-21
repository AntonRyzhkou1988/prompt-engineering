using PromptEngineering.LLM.Models;

namespace PromptEngineering.Services;

public sealed record ContextPipelineResult(string OutputPath, ChatCompletion Completion);
