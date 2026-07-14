namespace WiseAutoShutdown.Core.Runtime;

public interface IRestrictionPrompt
{
    PromptResult Show(PromptRequest request);
}
