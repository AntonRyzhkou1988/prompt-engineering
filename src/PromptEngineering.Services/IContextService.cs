namespace PromptEngineering.Services;

/// <summary>
/// Executes the prompt-engineering (ReAct) pipeline:
/// dataset load, prompt construction, LLM completion, and persisting the first choice assistant message as Markdown
/// for each prompt version defined in <see cref="ContextSettings.ReActSequence"/>.
/// </summary>
public interface IContextService
{
    /// <summary>
    /// Runs each prompt file in <see cref="ContextSettings.ReActSequence"/> once in sequence.
    /// After each run the first-choice completion is injected as <c>&lt;prior_run&gt;</c>
    /// into the next prompt, enabling cross-version ReAct chaining (for example v1 → v2 → v3 → answer).
    /// Prompts that contain no <c>&lt;prior_run&gt;...&lt;/prior_run&gt;</c> region silently ignore the injected content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="ContextPipelineResult"/> per prompt file, in execution order.</returns>
    Task<IReadOnlyList<ContextPipelineResult>> RunReActAsync(CancellationToken cancellationToken = default);
}
