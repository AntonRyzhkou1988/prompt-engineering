namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering (ReAct) pipeline:
/// dataset load, prompt construction, LLM completion, and persisting the first choice assistant message as Markdown
/// for each prompt JSON discovered in <see cref="ContextSettings.PromptPath"/>.
/// </summary>
public interface IContextService
{
    /// <summary>
    /// Runs the single-turn ReAct pipeline for all prompt JSON files discovered from
    /// <see cref="ContextSettings.PromptPath"/> in file-name order.
    /// Each run injects dataset rows into the prompt <c>&lt;data&gt;...&lt;/data&gt;</c> region as <c>&lt;record&gt;</c> XML.
    /// </summary>
    /// <returns>One <see cref="ContextPipelineResult"/> per execution (always at least one item when successful).</returns>
    Task<IReadOnlyList<ContextPipelineResult>> RunReActAsync(CancellationToken cancellationToken = default);
}
