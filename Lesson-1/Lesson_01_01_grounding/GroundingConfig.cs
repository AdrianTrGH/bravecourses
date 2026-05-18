using Microsoft.Extensions.Configuration;

namespace Lesson_1.Lesson_01_01_grounding;

internal class GroundingConfig
{
    internal string ApiKey { get; }
    internal string Endpoint { get; }
    internal string Model { get; }
    internal string SearchModel { get; }

    internal string OutputDir { get; }
    internal string NotesDir { get; }
    internal string TemplatePath { get; }
    internal string ConceptsPath { get; }
    internal string DedupePath { get; }
    internal string SearchPath { get; }
    internal string GroundedPath { get; }

    internal bool Force { get; }
    internal int BatchSize { get; }
    internal string? InputFile { get; }

    internal GroundingConfig(IConfiguration config, string[] args)
    {
        ApiKey = config["AI_API_KEY"] ?? throw new InvalidOperationException("AI_API_KEY not set");
        Endpoint = config["RESPONSES_API_ENDPOINT"] ?? "https://openrouter.ai/api/v1/responses";
        var baseModel = config["GROUND_MODEL"] ?? config["AI_MODEL"] ?? "gpt-4o-mini";
        Model = baseModel;
        SearchModel = baseModel.EndsWith(":online") ? baseModel : baseModel + ":online";

        var baseDir = AppContext.BaseDirectory;
        var lesson01Dir = Path.Combine(baseDir, "Lesson_01_01_grounding");
        OutputDir = Path.Combine(lesson01Dir, "output");
        NotesDir = Path.Combine(lesson01Dir, "notes");
        TemplatePath = Path.Combine(lesson01Dir, "template.html");
        ConceptsPath = Path.Combine(OutputDir, "concepts.json");
        DedupePath = Path.Combine(OutputDir, "dedupe.json");
        SearchPath = Path.Combine(OutputDir, "search_results.json");
        GroundedPath = Path.Combine(OutputDir, "grounded.html");

        Force = args.Contains("--force");
        InputFile = args.FirstOrDefault(a => !a.StartsWith("--"));
        BatchSize = ParseBatchSize(args);
    }

    private static int ParseBatchSize(string[] args)
    {
        if (args.Contains("--no-batch")) return 1;
        var batchArg = args.FirstOrDefault(a => a.StartsWith("--batch="));
        if (batchArg != null && int.TryParse(batchArg["--batch=".Length..], out var n) && n >= 1)
            return Math.Min(n, 10);
        return 3;
    }

    internal string ResolveInputPath()
    {
        if (InputFile == null)
        {
            var defaults = Directory.Exists(NotesDir)
                ? Directory.GetFiles(NotesDir, "*.md")
                : Array.Empty<string>();
            return defaults.Length > 0
                ? defaults[0]
                : throw new InvalidOperationException("No input file specified and no .md files found in notes/");
        }

        if (File.Exists(InputFile)) return InputFile;

        var inNotes = Path.Combine(NotesDir, InputFile);
        if (File.Exists(inNotes)) return inNotes;

        throw new FileNotFoundException($"Input file not found: {InputFile}");
    }
}
