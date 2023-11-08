using Ckeisc.OpenAi;

namespace PlanningTest;

public class CreateIllustrationFunction : Function
{
    public CreateIllustrationFunction(string prompt = "") : base(
        "Create an illustration image file based on a given prompt",
        typeof(string)
    )
    {
        // initializing input is important for tooling to understand the expected input and type
        this.Inputs["prompt"] = prompt;
    }

    protected override List<object> ExtractResult(string input)
    {
        return new List<object> { input };
    }

    protected override async Task<string> RunPrompt()
    {
        OpenAiClient client = GetDependency<OpenAiClient>();
        string prompt = (string)(this.Inputs["prompt"] ?? "");
        string imagePath = Path.Combine(
            Path.GetTempPath(), $"{Path.GetTempFileName()}.png");

        ImageResponse response = await client.CreateImage(prompt, ImageResponseFormat.Base64Json);
        if (response != null)
        {
            string? base64String = response.Data.FirstOrDefault()?.Base64Json;
            if (base64String != null)
            {
                byte[] bytes = Convert.FromBase64String(base64String);
                File.WriteAllBytes(imagePath, bytes);
            }
        }

        return imagePath;
    }

    public async Task<string> RunAndGetResult()
    {
        await this.Run();
        return (string)this.Results[0];
    }
}
