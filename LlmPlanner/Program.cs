// load param from JSON - openai request format, sys, user, asst
// planner like function, how to pipe result from one step to another

using Ckeisc.OpenAi;
using PlanningTest;

internal class Program
{
    const string ApiKey = "";

    private static async Task Main(string[] args)
    {
        Function.InjectDependency<OpenAiClient>(() =>  new OpenAiClient(ApiKey));

        List<Function> functions = new()
        {
            new PlanBookChaptersFunction(),
            new WriteBookChapterFunction(),
            new CreateIllustrationPromptFunction(),
            new CreateIllustrationFunction()
        };

        Planner planner = new(
            "create a children's story book about a genius dog " + 
            "saving all the pizza in the wrold from being kidnapped by evil professor cat " +
            "with 3 chapters, each chapter should contain 3 acts and " + 
            "with illustration for each chapter based on chapter synopsis.", 
            functions);

        string result = await planner.RunAndGetResults();
    }

    private static async Task InterpreterTest()
    {
        List<Function> functions = new()
        {
            new PlanBookChaptersFunction(),
            new WriteBookChapterFunction(),
            new CreateIllustrationPromptFunction(),
            new CreateIllustrationFunction()
        };

        string code = File.ReadAllText("sample.code");
        CodeInterpreter codeInterpreter = new();
        string result = await codeInterpreter.Run(code, functions);
    }

    private static async Task FunctionTest()
    {
        string bookTheme = "a chef cat's adventure";

        PlanBookChaptersFunction func1 = new PlanBookChaptersFunction(bookTheme, 5);

        List<BookChapter> chapters = await func1.RunAndGetResults();

        WriteBookChapterFunction func2 = new WriteBookChapterFunction(bookTheme, chapters[0].Name, 5);

        string chapterText = await func2.RunAndGetResults();

        CreateIllustrationPromptFunction func3 = new CreateIllustrationPromptFunction(chapters[0].Synopsis);
        string prompt = await func3.RunAndGetResults();

        CreateIllustrationFunction func4 = new CreateIllustrationFunction(prompt);
        string filename = await func4.RunAndGetResult();
    }
}