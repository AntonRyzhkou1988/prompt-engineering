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

    /// <summary>
    /// Writes a single text file aggregating the first-choice assistant <c>Message.Content</c> from each
    /// <see cref="ContextPipelineResult"/> in <paramref name="runs"/> order, separated by a fixed <c>---</c> block.
    /// Runs with empty content are represented as an explicit placeholder referencing <see cref="ContextPipelineResult.OutputPath"/>.
    /// </summary>
    /// <remarks>
    /// Does not invoke the LLM and does not add analytical claims; host-side formatting only for human comparison
    /// (for example v1/v2/v3 side by side). Default <paramref name="outputPath"/> is <c>results.txt</c> (relative to the process working directory).
    /// </remarks>
    Task SummarizeAsync(
        IReadOnlyList<ContextPipelineResult> runs,
        string outputPath = "results.txt",
        CancellationToken cancellationToken = default);
}
