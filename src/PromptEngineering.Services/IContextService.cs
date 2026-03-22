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
    /// Runs a single prompt file through <paramref name="iterations"/> chained ReAct cycles.
    /// After each cycle the first-choice completion is injected into the prompt's
    /// <c>&lt;prior_run&gt;...&lt;/prior_run&gt;</c> region for the next iteration,
    /// enabling the model to build on and refine its prior analysis.
    /// </summary>
    /// <param name="promptFileName">
    /// File name (for example <c>initial.json</c>) resolved against <see cref="ContextSettings.PromptPath"/>.
    /// </param>
    /// <param name="iterations">Number of chained cycles to execute (must be at least 1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="ContextPipelineResult"/> per iteration.</returns>
    Task<IReadOnlyList<ContextPipelineResult>> RunIterativeAsync(
        string promptFileName,
        int iterations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs each prompt file in <paramref name="promptFileNames"/> once in sequence.
    /// After each run the first-choice completion is injected as <c>&lt;prior_run&gt;</c>
    /// into the next prompt, enabling cross-version ReAct chaining (for example v1 → v2 → v3).
    /// Prompts that contain no <c>&lt;prior_run&gt;...&lt;/prior_run&gt;</c> region silently ignore the injected content.
    /// </summary>
    /// <param name="promptFileNames">
    /// Ordered list of file names (for example <c>["v1.json", "v2.json", "v3.json"]</c>)
    /// resolved against <see cref="ContextSettings.PromptPath"/>. Must contain at least one entry.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="ContextPipelineResult"/> per prompt file, in execution order.</returns>
    Task<IReadOnlyList<ContextPipelineResult>> RunVersionChainAsync(
        IReadOnlyList<string> promptFileNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a single text file aggregating the first-choice assistant <c>Message.Content</c> from each
    /// <see cref="ContextPipelineResult"/> in <paramref name="runs"/> order. Each run is prefixed with a compact header
    /// (<c>## Run: {PromptStem}</c> and <c>Output: {OutputPath}</c>); runs are separated by a fixed <c>---</c> block.
    /// Runs with empty content are represented as an explicit placeholder referencing <see cref="ContextPipelineResult.OutputPath"/>.
    /// </summary>
    /// <remarks>
    /// Does not invoke the LLM and does not add analytical claims; host-side formatting only for human comparison.
    /// Output is written to <c>summarize.txt</c> inside <see cref="ContextSettings.OutputDirectory"/>.
    /// </remarks>
    Task SummarizeAsync(
        IReadOnlyList<ContextPipelineResult> runs,
        CancellationToken cancellationToken = default);
}
