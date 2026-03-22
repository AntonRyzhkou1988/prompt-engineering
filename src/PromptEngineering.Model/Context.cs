namespace PromptEngineering.Model;

/// <summary>
/// A configurable key/value entry used when assembling LLM prompt context (for example system role text).
/// </summary>
/// <param name="Key">Stable identifier for this context entry.</param>
/// <param name="Value">Text bound to the key (often loaded from configuration).</param>
public sealed record Context(string Key, string Value)
{
    /// <summary>
    /// Well-known keys for prompt-engineering context entries.
    /// Keep <see cref="ContextKeys"/> values aligned with <c>ContextSettings.AssistantRoleKey</c> in configuration.
    /// </summary>
    public static class ContextKeys
    {
        public const string AssistantRole = "assistant.role";
    }
}
