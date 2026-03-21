namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering (ReAct) pipeline:
/// dataset load, prompt construction, LLM completion, and persisting the first choice assistant message as Markdown.
/// </summary>
public interface IContextService
{
    /// <summary>
    /// Runs the ReAct pipeline. <paramref /> resolution: <c>null</c> or whitespace runs once using prompts
    /// already loaded from <see cref="ContextSettings.PromptPath"/> (must be an absolute JSON file path in configuration).
    /// Otherwise resolves to an existing path: if it is a JSON file, runs once for that file; if it is a directory,
    /// discovers all <c>*.json</c> there and runs each in file-name order with console progress. Default <c>prompts</c>
    /// targets the prompts folder next to the app / repo layout.
    /// </summary>
    /// <returns>One <see cref="ContextPipelineResult"/> per execution (always at least one item when successful).</returns>
    Task<IReadOnlyList<ContextPipelineResult>> RunReActAsync(CancellationToken cancellationToken = default);
}
