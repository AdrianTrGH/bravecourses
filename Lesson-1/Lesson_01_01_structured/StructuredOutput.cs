using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lesson_1.Lesson_01_01_structured;

internal static class StructuredOutput
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions JsonReadOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly object PersonSchema = new
    {
        type = "json_schema",
        name = "person",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                name = new { type = (object)new[] { "string", "null" }, description = "Full name of the person. Use null if not mentioned." },
                age = new { type = (object)new[] { "number", "null" }, description = "Age in years. Use null if not mentioned or unclear." },
                occupation = new { type = (object)new[] { "string", "null" }, description = "Job title or profession. Use null if not mentioned." },
                skills = new { type = "array", items = new { type = "string" }, description = "List of skills, technologies, or competencies. Empty array if none mentioned." }
            },
            required = new[] { "name", "age", "occupation", "skills" },
            additionalProperties = false
        }
    };

    internal static async Task Run()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var apiKey = config["AI_API_KEY"] ?? throw new InvalidOperationException("AI_API_KEY not set");
        var endpoint = config["RESPONSES_API_ENDPOINT"] ?? "https://openrouter.ai/api/v1/responses";
        var model = config["AI_MODEL"] ?? "gpt-4o-mini";

        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var text = "John is 30 years old and works as a software engineer. He is skilled in JavaScript, Python, and React.";

        try
        {
            var person = await ExtractPerson(endpoint, model, text);

            Console.WriteLine($"Input:      {text}\n");
            Console.WriteLine($"Name:       {person.Name ?? "unknown"}");
            Console.WriteLine($"Age:        {person.Age?.ToString() ?? "unknown"}");
            Console.WriteLine($"Occupation: {person.Occupation ?? "unknown"}");
            Console.WriteLine($"Skills:     {(person.Skills?.Length > 0 ? string.Join(", ", person.Skills) : "none")}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<PersonResult> ExtractPerson(string endpoint, string model, string text)
    {
        var body = new
        {
            model,
            input = $"Extract person information from: \"{text}\"",
            text = new { format = PersonSchema }
        };

        var json = JsonSerializer.Serialize(body, JsonOpts);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await Http.PostAsync(endpoint, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        root.TryGetProperty("error", out var err);
        if (!response.IsSuccessStatusCode || err.ValueKind == JsonValueKind.Object)
        {
            var msg = err.ValueKind == JsonValueKind.Object && err.TryGetProperty("message", out var m)
                ? m.GetString()
                : $"HTTP {(int)response.StatusCode}";
            throw new Exception(msg);
        }

        var outputText = ExtractText(root);
        return JsonSerializer.Deserialize<PersonResult>(outputText, JsonReadOpts)
               ?? throw new Exception("Failed to parse person data");
    }

    private static string ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(ot.GetString()))
            return ot.GetString()!;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new Exception("No text output in response");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var t) || t.GetString() != "message") continue;
            if (!item.TryGetProperty("content", out var c) || c.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in c.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var pt) && pt.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                    return text.GetString() ?? throw new Exception("Empty text");
            }
        }

        throw new Exception("No text output in response");
    }

    private record PersonResult(string? Name, double? Age, string? Occupation, string[]? Skills);
}
