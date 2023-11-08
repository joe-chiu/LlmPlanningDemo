using Ckeisc.OpenAi;

namespace PlanningTest;

// input: theme of the book
// output: a list of chapter names
public class CreateIllustrationPromptFunction : LlmFunction
{
    public CreateIllustrationPromptFunction(string synopsis = "") : base(
        "Create a text prompt for creating illustration from a short synopsis",
        typeof(string)
    )
    {
        // initializing input is important for tooling to understand the expected input and type
        this.Inputs["synopsis"] = synopsis;
    }

    protected override List<object> ExtractResult(string input)
    {
        return new List<object> { input };
    }

    public async Task<string> RunAndGetResults()
    {
        await this.Run();
        return (string)this.Results[0];
    }
}
