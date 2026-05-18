using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lesson_1.Lesson_01_01_grounding;

internal class ApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions JsonReadOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _endpoint;
    private const int Retries = 3;
    private const int RetryDelayMs = 1000;
    private static readonly int[] RetryStatuses = [429, 500, 502, 503];

    internal ApiClient(string apiKey, string endpoint)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(180_000) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _endpoint = endpoint;
    }

    internal async Task<JsonDocument> Chat(
        string model,
        object input,
        object? textFormat = null,
        object[]? tools = null,
        object? reasoning = null)
    {
        var body = BuildRequestBody(model, input, textFormat, tools, reasoning);
        var json = JsonSerializer.Serialize(body, JsonOpts);

        for (var attempt = 1; attempt <= Retries; attempt++)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(_endpoint, content);
            }
            catch (Exception ex) when (attempt < Retries)
            {
                Console.Error.WriteLine($"    [retry {attempt}/{Retries}] request error: {ex.Message}");
                await Task.Delay(RetryDelayMs * attempt);
                continue;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            if (RetryStatuses.Contains((int)response.StatusCode) && attempt < Retries)
            {
                Console.Error.WriteLine($"    [retry {attempt}/{Retries}] status {(int)response.StatusCode}");
                await Task.Delay(RetryDelayMs * attempt);
                continue;
            }

            doc.RootElement.TryGetProperty("error", out var errProp);
            if (!response.IsSuccessStatusCode || errProp.ValueKind == JsonValueKind.Object)
            {
                var msg = errProp.ValueKind == JsonValueKind.Object && errProp.TryGetProperty("message", out var m)
                    ? m.GetString()
                    : $"HTTP {(int)response.StatusCode}";
                throw new Exception($"API error: {msg}");
            }

            return doc;
        }

        throw new Exception("Max retries exceeded");
    }

    internal static string ExtractText(JsonDocument doc)
    {
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
        {
            var t = ot.GetString();
            if (!string.IsNullOrWhiteSpace(t)) return t!;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new Exception("No text output in API response");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var type) || type.GetString() != "message") continue;
            if (!item.TryGetProperty("content", out var contentArr) || contentArr.ValueKind != JsonValueKind.Array) continue;

            foreach (var part in contentArr.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var pt) && pt.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                    return text.GetString() ?? throw new Exception("Empty text in output");
            }
        }

        throw new Exception("No text output in API response");
    }

    internal static T ExtractJson<T>(JsonDocument doc, string context)
    {
        var text = ExtractText(doc);
        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonReadOpts)
                ?? throw new Exception($"Null result for {context}");
        }
        catch (JsonException ex)
        {
            throw new Exception($"JSON parse error for {context}: {ex.Message}\nOutput: {Truncate(text, 300)}");
        }
    }

    internal static List<SearchSourceRaw> ExtractWebSources(JsonDocument doc)
    {
        var sources = new List<SearchSourceRaw>();
        var seen = new HashSet<string>();

        void Walk(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray()) Walk(item);
            }
            else if (el.ValueKind == JsonValueKind.Object)
            {
                if (el.TryGetProperty("type", out var t) && t.GetString() == "web_search_call"
                    && el.TryGetProperty("action", out var action)
                    && action.TryGetProperty("sources", out var srcs))
                {
                    foreach (var s in srcs.EnumerateArray())
                    {
                        var url = s.TryGetProperty("url", out var u) ? u.GetString() : null;
                        var title = s.TryGetProperty("title", out var tt) ? tt.GetString() : null;
                        if (url != null && seen.Add(url))
                            sources.Add(new SearchSourceRaw(title, url));
                    }
                }

                if (el.TryGetProperty("type", out var ct) && ct.GetString() == "url_citation"
                    && el.TryGetProperty("url", out var cu))
                {
                    var url = cu.GetString();
                    var title = el.TryGetProperty("title", out var ctt) ? ctt.GetString() : null;
                    if (url != null && seen.Add(url))
                        sources.Add(new SearchSourceRaw(title, url));
                }

                foreach (var prop in el.EnumerateObject()) Walk(prop.Value);
            }
        }

        Walk(doc.RootElement);
        return sources;
    }

    private static object BuildRequestBody(string model, object input, object? textFormat, object[]? tools, object? reasoning)
    {
        var dict = new Dictionary<string, object>
        {
            ["model"] = model,
            ["input"] = input
        };
        if (textFormat != null)
            dict["text"] = new { format = textFormat };
        if (tools != null)
            dict["tools"] = tools;
        if (reasoning != null)
            dict["reasoning"] = reasoning;
        return dict;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 3)] + "...";
}

internal record SearchSourceRaw(string? Title, string Url);
