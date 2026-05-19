using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lesson_1.Lesson_01_Task;

internal static class TaskPeople
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions JsonReadOpts = new() { PropertyNameCaseInsensitive = true };

    private const int CurrentYear = 2026;
    private const int MinAge = 20;
    private const int MaxAge = 40;

    private static readonly string[] AllowedTags =
        ["IT", "transport", "edukacja", "medycyna", "praca z ludźmi", "praca z pojazdami", "praca fizyczna"];

    private static readonly object TagSchema = new
    {
        type = "json_schema",
        name = "job_tags",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                results = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "number" },
                            tags = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "id", "tags" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "results" },
            additionalProperties = false
        }
    };

    internal static async Task Run()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var apiKey = config["AI_API_KEY"] ?? throw new InvalidOperationException("AI_API_KEY not set");
        var hubKey = config["HUB_API_KEY"] ?? throw new InvalidOperationException("HUB_API_KEY not set");
        var endpoint = config["RESPONSES_API_ENDPOINT"] ?? "https://openrouter.ai/api/v1/responses";
        var model = config["AI_MODEL"] ?? "gpt-4o-mini";

        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var csvPath = ResolveCsvPath();
        Console.WriteLine($"Reading: {csvPath}");

        var people = ReadCsv(csvPath);
        Console.WriteLine($"Total records: {people.Count}");

        var candidates = people
            .Where(p => p.Gender == "M"
                        && p.BirthPlace == "Grudziądz"
                        && p.BornYear >= CurrentYear - MaxAge
                        && p.BornYear <= CurrentYear - MinAge)
            .ToList();
        Console.WriteLine($"After filter (M + Grudziądz + age {MinAge}-{MaxAge}): {candidates.Count}");

        Console.WriteLine("Tagging jobs with LLM...");
        var tagMap = await BatchTagJobs(endpoint, model, candidates);

        var transport = candidates
            .Where(p => tagMap.TryGetValue(p.Id, out var tags) && tags.Contains("transport"))
            .ToList();
        Console.WriteLine($"With 'transport' tag: {transport.Count}");

        foreach (var p in transport)
        {
            var tags = tagMap[p.Id];
            Console.WriteLine($"  {p.Name} {p.Surname} ({p.BornYear}) [{string.Join(", ", tags)}]");
        }

        var answer = transport.Select(p => new AnswerPerson(
            p.Name, p.Surname, p.Gender, p.BornYear, p.BirthPlace, tagMap[p.Id]
        )).ToList();

        Console.WriteLine("\nSending answer...");
        var flag = await SubmitAnswer(hubKey, answer);
        Console.WriteLine($"\nResponse: {flag}");
    }

    private static async Task<Dictionary<int, List<string>>> BatchTagJobs(
        string endpoint, string model, List<Person> people)
    {
        var lines = people.Select((p, i) => $"{i}: {p.Job}");
        var jobList = string.Join("\n", lines);

        var prompt = $$"""
            Przypisz tagi do każdego z poniższych opisów stanowisk pracy.
            Używaj WYŁĄCZNIE tagów z tej listy (możliwe kilka na rekord):
            - IT: programowanie, systemy komputerowe, sieci, software, hardware
            - transport: kierowcy, logistyka, spedycja, kurierzy, operatorzy pojazdów, zarządzanie flotą, dostawy, przewóz ładunków
            - edukacja: nauczyciele, trenerzy, instruktorzy, wykładowcy
            - medycyna: lekarze, pielęgniarki, farmaceuci, ratownicy, diagnostyka, leczenie
            - praca z ludźmi: obsługa klienta, HR, psychologia, opieka, doradztwo
            - praca z pojazdami: mechanicy, serwis, naprawa pojazdów, operatorzy maszyn
            - praca fizyczna: budowa, montaż, produkcja, obróbka materiałów, instalacje

            Dla każdego rekordu zwróć jego numer (id) i tablicę pasujących tagów.
            Jeśli żaden tag nie pasuje, zwróć pustą tablicę.

            Stanowiska:
            {{jobList}}
            """;

        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["input"] = new[] { new { role = "user", content = prompt } },
            ["text"] = new { format = TagSchema }
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
                ? m.GetString() : $"HTTP {(int)response.StatusCode}";
            throw new Exception($"API error: {msg}");
        }

        var outputText = ExtractText(root);
        var result = JsonSerializer.Deserialize<TagResults>(outputText, JsonReadOpts)
                     ?? throw new Exception("Failed to parse tag results");

        return result.Results.ToDictionary(
            r => people[r.Id].Id,
            r => r.Tags.Where(t => AllowedTags.Contains(t)).ToList()
        );
    }

    private static async Task<string> SubmitAnswer(string hubKey, List<AnswerPerson> answer)
    {
        var payload = new { apikey = hubKey, task = "people", answer };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var httpMsg = new HttpRequestMessage(HttpMethod.Post, "https://hub.ag3nts.org/verify");
        httpMsg.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await Http.SendAsync(httpMsg);
        return await response.Content.ReadAsStringAsync();
    }

    private static string ResolveCsvPath()
    {
        var paths = new[]
        {
            "people.csv",
            Path.Combine(AppContext.BaseDirectory, "people.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "people.csv"),
        };
        return paths.FirstOrDefault(File.Exists)
               ?? throw new FileNotFoundException("people.csv not found");
    }

    private static List<Person> ReadCsv(string path)
    {
        var people = new List<Person>();
        var lines = File.ReadAllLines(path);

        for (var i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Length < 7) continue;

            if (!DateTime.TryParse(fields[3], out var birthDate)) continue;

            people.Add(new Person(
                Id: i,
                Name: fields[0].Trim(),
                Surname: fields[1].Trim(),
                Gender: fields[2].Trim(),
                BornYear: birthDate.Year,
                BirthPlace: fields[4].Trim(),
                Job: fields[6].Trim('"', ' ')
            ));
        }

        return people;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private static string ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var ot) && !string.IsNullOrWhiteSpace(ot.GetString()))
            return ot.GetString()!;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new Exception("No output in response");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var t) || t.GetString() != "message") continue;
            if (!item.TryGetProperty("content", out var c)) continue;
            foreach (var part in c.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var pt) && pt.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                    return text.GetString()!;
            }
        }

        throw new Exception("No text in response");
    }

    private record Person(int Id, string Name, string Surname, string Gender, int BornYear, string BirthPlace, string Job);
    private record AnswerPerson(string Name, string Surname, string Gender, int Born, string City, List<string> Tags);
    private record TagResult(int Id, List<string> Tags);
    private record TagResults(List<TagResult> Results);
}
