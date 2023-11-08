using Ckeisc.OpenAi;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanningTest;

public abstract class LlmFunction : Function
{
    protected ChatMessage[] prompt;
    protected List<string>? loopVariable;

    public LlmFunction(
        string description,
        Type resultType,
        List<string>? loopVariable = null,
        bool isResultList = false) : base(description, resultType, isResultList)
    {
        this.Description = description;
        this.loopVariable = loopVariable;
        this.prompt = new ChatMessage[0];
    }

    protected override async Task<string> RunPrompt()
    {
        string jsonFilename = $"{this.GetType().Name}.json";
        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase)
            }
        };

        ChatMessage[]? messages = JsonSerializer.Deserialize<ChatMessage[]>(File.ReadAllText(jsonFilename), jsonOptions);
        this.prompt = messages ?? new ChatMessage[0];

        string template = this.prompt.Last().Content;
        foreach (KeyValuePair<string, string> pair in this.Inputs)
        {
            if (pair.Value != null)
            {
                template = template.Replace(
                    "{" + pair.Key + "}", pair.Value);
            }
        }
        this.prompt.Last().Content = template;

        OpenAiClient client = GetDependency<OpenAiClient>();
        if (this.loopVariable == null)
        {
            ChatCompletionResponse response = await client.CreateChat(this.prompt, maxTokens: 2048);
            return response.Choices[0].Message.Content;
        }
        else
        {
            StringBuilder sb = new();
            for (int i = 0; i < loopVariable.Count; i++)
            {
                string loopTemplate = this.prompt.Last().Content;
                this.prompt.Last().Content = loopTemplate.Replace("{i}", $"{loopVariable[i]}");
                ChatCompletionResponse response = await client.CreateChat(this.prompt, maxTokens: 2048);
                // restore the template for the next iteration
                this.prompt.Last().Content = loopTemplate;
                string content = response.Choices[0].Message.Content;
                sb.Append(content);
            }

            return sb.ToString();
        }
    }
}
