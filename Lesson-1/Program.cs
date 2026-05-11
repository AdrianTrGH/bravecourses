using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lesson_1;

internal class Program
{
    private static readonly HttpClient HttpClient = new();
    private static string _endpoint = "";
    private static string _model = "";

    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var apiKey = config["AI_API_KEY"]
            ?? throw new InvalidOperationException("AI_API_KEY is not set");
        _endpoint = config["RESPONSES_API_ENDPOINT"] ?? "https://api.openai.com/v1/responses";
        _model = config["AI_MODEL"] ?? "o4-mini";

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var firstQuestion = "What is 25 * 48?";
            var (firstText, firstReasoningTokens) = await Chat(firstQuestion);

            var secondQuestion = "Divide that by 4.";
            object[] history =
            [
                new { type = "message", role = "user", content = firstQuestion },
                new { type = "message", role = "assistant", content = firstText }
            ];
            var (secondText, secondReasoningTokens) = await Chat(secondQuestion, history);

            Console.WriteLine($"Q: {firstQuestion}");
            Console.WriteLine($"A: {firstText} ({firstReasoningTokens} reasoning tokens)");
            Console.WriteLine($"Q: {secondQuestion}");
            Console.WriteLine($"A: {secondText} ({secondReasoningTokens} reasoning tokens)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task<(string Text, int ReasoningTokens)> Chat(string input, object[]? history = null)
    {
        List<object> messages = [.. history ?? [], new { type = "message", role = "user", content = input }];

        var body = new { model = _model, input = messages, reasoning = new { effort = "medium" } };
        var json = JsonSerializer.Serialize(body);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(_endpoint, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!response.IsSuccessStatusCode || root.TryGetProperty("error", out _))
        {
            var errorMessage = root.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : $"Request failed with status {(int)response.StatusCode}";
            throw new Exception(errorMessage);
        }

        var text = ExtractResponseText(root);
        if (string.IsNullOrEmpty(text))
            throw new Exception("Missing text output in API response");

        var reasoningTokens = 0;
        if (root.TryGetProperty("usage", out var usage)
            && usage.TryGetProperty("output_tokens_details", out var details)
            && details.TryGetProperty("reasoning_tokens", out var rt))
        {
            reasoningTokens = rt.GetInt32();
        }

        return (text, reasoningTokens);
    }

    private static string ExtractResponseText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText)
            && outputText.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(outputText.GetString()))
        {
            return outputText.GetString()!;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var type) || type.GetString() != "message")
                continue;
            if (!item.TryGetProperty("content", out var contentArr) || contentArr.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in contentArr.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var partType) && partType.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "";
                }
            }
        }

        return "";
    }
}
