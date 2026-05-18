using Microsoft.Extensions.Configuration;
using Lesson_1.Lesson_01_01_grounding.Pipeline;
using Lesson_1.Lesson_01_01_grounding;
using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Lesson_01_01_grounding;

internal static class Grounding
{
    internal static async Task Run(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var cfg = new GroundingConfig(config, args);
        var api = new ApiClient(cfg.ApiKey, cfg.Endpoint);

        if (!ConfirmRun()) return;

        try
        {
            var inputPath = cfg.ResolveInputPath();
            var sourceFile = Path.GetFileName(inputPath);
            var markdown = await File.ReadAllTextAsync(inputPath);
            var paragraphs = TextUtils.SplitParagraphs(markdown);

            Console.WriteLine($"\nSource: {sourceFile} ({paragraphs.Count} paragraphs)");

            Console.WriteLine("\n[1/4] Extracting concepts...");
            var extractor = new ConceptExtractor(api, cfg);
            var conceptsData = await extractor.ExtractConcepts(paragraphs, sourceFile);
            Console.WriteLine($"  → {conceptsData.ParagraphCount} paragraphs, {conceptsData.ConceptCount} concepts");

            Console.WriteLine("\n[2/4] Deduplicating concepts...");
            var deduper = new ConceptDeduper(api, cfg);
            var dedupeData = await deduper.DedupeConcepts(conceptsData);
            Console.WriteLine($"  → {dedupeData.Groups.Count} canonical groups");

            Console.WriteLine("\n[3/4] Searching concepts...");
            var searcher = new ConceptSearcher(api, cfg);
            var searchData = await searcher.SearchConcepts(conceptsData, dedupeData);
            Console.WriteLine($"  → {searchData.ResultsByCanonical.Count} results");

            Console.WriteLine("\n[4/4] Grounding HTML...");
            var grounder = new HtmlGrounder(api, cfg);
            await grounder.GenerateAndApplyTemplate(paragraphs, conceptsData, dedupeData, searchData);

            Console.WriteLine($"\nDone! Output files:");
            Console.WriteLine($"  concepts.json  → {cfg.ConceptsPath}");
            Console.WriteLine($"  dedupe.json    → {cfg.DedupePath}");
            Console.WriteLine($"  search.json    → {cfg.SearchPath}");
            Console.WriteLine($"  grounded.html  → {cfg.GroundedPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nError: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static bool ConfirmRun()
    {
        Console.WriteLine("UWAGA: Ten pipeline wykonuje wiele zapytań do API i może zużyć znaczną liczbę tokenów.");
        Console.Write("Czy chcesz kontynuować? [t/N] ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        return answer is "t" or "tak" or "y" or "yes";
    }
}
