using Ckeisc.OpenAi;
using System.Text.RegularExpressions;

namespace PlanningTest;

// input: theme of the book
// output: a list of chapter names
public class PlanBookChaptersFunction : LlmFunction
{
    public PlanBookChaptersFunction(string bookTheme = "", int chapterCount = 3) : base(
        "Create a list of chapter names and synopsis given a book theme as input",
        typeof(BookChapter),
        isResultList: true
    )
    {
        this.Inputs["bookTheme"] = bookTheme;
        this.Inputs["chapterCount"] = $"{chapterCount}";
    }

    protected override List<object> ExtractResult(string input)
    {
        Regex pattern = new Regex(@"\[(.+)\]\s+<(.+)>");
        MatchCollection matches = pattern.Matches(input);
        List<BookChapter> list = matches.Select(match =>
            new BookChapter(match.Groups[1].Value, match.Groups[2].Value)).ToList();
        return list.ConvertAll(item => (object)item);
    }

    public async Task<List<BookChapter>> RunAndGetResults()
    {
        await this.Run();
        return this.Results.ConvertAll(item => (BookChapter)item);
    }
}

public class BookChapter
{
    public string Name { get; private set; }
    public string Synopsis { get; private set; }

    public BookChapter(string name, string synopsis)
    {
        this.Name = name;
        this.Synopsis = synopsis;
    }
}