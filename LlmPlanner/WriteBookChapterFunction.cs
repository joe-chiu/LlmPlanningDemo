using Ckeisc.OpenAi;
using System.Text.RegularExpressions;

namespace PlanningTest;

public class WriteBookChapterFunction : LlmFunction
{
    public WriteBookChapterFunction(
        string bookTheme = "", 
        string chapterName = "",
        int actCount = 5) : base(
            "Create content for multiple acts within a chapter given a book theme and chapter name as input",
            typeof(string)
    )
    {
        this.Inputs["bookTheme"] = bookTheme;
        this.Inputs["chapterName"] = chapterName;
        this.Inputs["actCount"] = $"{actCount}";
    }

    protected override Task<string> RunPrompt()
    {
        int loopCount = int.Parse(this.Inputs["actCount"]);
        this.loopVariable = new();
        for (int i=0; i<loopCount; i++)
        {
            this.loopVariable.Add((i + 1).ToString());
        }
        return base.RunPrompt();
    }

    protected override List<object> ExtractResult(string input)
    {
        Regex pattern = new Regex(@"\[(.+)\]\s+<(.+)>");

        MatchCollection matches = pattern.Matches(input);

        List<string> list = matches.Select(match =>
            match.Groups[2].Value).ToList();

        string output = string.Join("\n\n", list);

        return new List<object> { output };
    }

    public async Task<string> RunAndGetResults()
    {
        await this.Run();
        return (string)this.Results[0];
    }
}
