using System.Text.Json;

namespace Lesson_1.Lesson_01_01_grounding.Utils;

internal static class FileUtils
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions JsonReadOpts = new() { PropertyNameCaseInsensitive = true };

    internal static void EnsureDir(string path) => Directory.CreateDirectory(path);

    internal static T? ReadJsonIfExists<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonReadOpts);
        }
        catch
        {
            return null;
        }
    }

    internal static async Task SafeWriteJson<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOpts);
        await File.WriteAllTextAsync(path, json);
    }
}
