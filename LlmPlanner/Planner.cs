using Ckeisc.OpenAi;
using System.Text;
using System.Text.RegularExpressions;

namespace PlanningTest;

public class Planner : LlmFunction
{
    protected List<Function> availableFunctions;

    public Planner(
        string goal,
        List<Function> availableFunctions
    ) : base(
        "Take in a goal and a list of available functions, create a plan in the form of code and execute by calling the functions",
        typeof(string)
    )
    {
        this.availableFunctions = availableFunctions;
        this.Inputs["goal"] = goal;
        this.Inputs["functionInfo"] = ProduceFunctionInfo(availableFunctions);
    }

    protected static string ProduceFunctionInfo(List<Function> functions)
    {
        StringBuilder sb = new();
        foreach(Function func in functions)
        {
            List<string> inputList = new();
            foreach (KeyValuePair<string, string> input in func.Inputs)
            {
                string entry = $"{input.Key}";
                if (input.Value != null)
                {
                    entry += $"({input.Value.GetType().Name})";
                }
                inputList.Add(entry);
            }
            string outputPrefix = func.IsResultList ? "a list of" : "a";
            string output =  $"{outputPrefix} {func.ResultType.Name}";
            string info = $"{func.GetType().Name}: {func.Description} " + 
                $"Input: {string.Join(", ", inputList)}. " + 
                $"Output: {output}";

            sb.Append(info + "\n");
        }

        return sb.ToString();
    }

    protected override List<object> ExtractResult(string input)
    {
        return new List<object> { input };
    }

    public async Task<string> RunAndGetResults()
    {
        Regex markdownPattern = new Regex("```\n([^`]+)\n```");

        await this.Run();
        string code = (string)this.Results[0];
        if (markdownPattern.IsMatch(code))
        {
            Match match = markdownPattern.Match(code);
            code = match.Groups[1].Value;
        }

        CodeInterpreter interpreter = new CodeInterpreter();
        string result = await interpreter.Run(code, this.availableFunctions);
        return result;
    }
}
