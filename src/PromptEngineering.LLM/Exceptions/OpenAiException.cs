namespace PromptEngineering.LLM.Exceptions;

[Serializable]
public class OpenAiException : Exception
{
    public OpenAiException()
    {
    }

    public OpenAiException(string message) : base(message)
    {
    }

    public OpenAiException(string message, Exception exception) : base(message, exception)
    {
    }
}
