using Lesson_1.Lesson_01_01_grounding;
using Lesson_1.Lesson_01_01_grounding.Models;
using Lesson_1.Lesson_01_01_grounding.Prompts;
using Lesson_1.Lesson_01_01_grounding.Schemas;
using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Lesson_01_01_grounding.Pipeline;

internal class ConceptExtractor(ApiClient api, GroundingConfig cfg)
{
    private static readonly string[] AllowedCategories =
        ["claim", "result", "method", "metric", "resource", "definition", "term", "entity", "reference"];

    internal async Task<ConceptsData> ExtractConcepts(List<string> paragraphs, string sourceFile)
    {
        FileUtils.EnsureDir(cfg.OutputDir);
        var sourceHash = HashUtils.HashText(string.Join("\n\n", paragraphs));
        var existing = FileUtils.ReadJsonIfExists<ConceptsData>(cfg.ConceptsPath);
        var shouldReuse = existing != null && existing.SourceFile == sourceFile && !cfg.Force;
        var sameHash = existing?.SourceHash == sourceHash;
        var sameModel = existing?.Model == cfg.Model;

        if (shouldReuse && sameHash && sameModel)
        {
            Console.WriteLine("   Using cached extract data");
            return existing!;
        }

        var data = shouldReuse ? existing! : new ConceptsData { SourceFile = sourceFile, Model = cfg.Model };
        var byIndex = data.Paragraphs.ToDictionary(p => p.Index, p => p);

        var pending = new List<(int Index, string Text, string Hash)>();
        for (var i = 0; i < paragraphs.Count; i++)
        {
            var hash = HashUtils.HashText(paragraphs[i]);
            if (byIndex.TryGetValue(i, out var cached) && cached.Hash == hash && !cfg.Force)
            {
                Console.WriteLine($"  [{i + 1}/{paragraphs.Count}] Cached");
                continue;
            }
            pending.Add((i, paragraphs[i], hash));
        }

        var currentIndices = Enumerable.Range(0, paragraphs.Count).ToHashSet();

        if (pending.Count == 0)
        {
            await Persist(data, byIndex, sourceHash, currentIndices);
            return data;
        }

        var batches = TextUtils.Chunk(pending, cfg.BatchSize);
        Console.WriteLine($"  Processing {pending.Count} paragraphs ({cfg.BatchSize} parallel)");

        foreach (var (batch, batchIdx) in batches.Select((b, i) => (b, i)))
        {
            var indices = string.Join(", ", batch.Select(x => x.Index + 1));
            Console.WriteLine($"  [batch {batchIdx + 1}/{batches.Count}] Paragraphs: {indices}");

            var results = await Task.WhenAll(batch.Select(item =>
                ExtractSingle(item.Index, item.Text, item.Hash, paragraphs.Count)));

            foreach (var result in results)
            {
                byIndex[result.Index] = result;
                Console.WriteLine($"    ✓ [{result.Index + 1}] {result.Concepts.Count} concepts");
            }

            await Persist(data, byIndex, sourceHash, currentIndices);
        }

        return data;
    }

    private async Task<ParagraphEntry> ExtractSingle(int index, string paragraph, string hash, int total)
    {
        var paragraphType = TextUtils.GetParagraphType(paragraph);
        var targetCount = TextUtils.GetTargetCount(paragraphType);
        var input = PromptBuilders.BuildExtractPrompt(paragraph, paragraphType, targetCount, index, total);

        var doc = await api.Chat(cfg.Model, input, JsonSchemas.Extract, reasoning: new { effort = "medium" });

        var result = ApiClient.ExtractJson<ExtractResult>(doc, $"extract paragraph {index + 1}");
        var filtered = FilterConcepts(result.Concepts ?? [], paragraph, paragraphType);

        return new ParagraphEntry { Index = index, Hash = hash, Text = paragraph, Concepts = filtered };
    }

    private static List<Concept> FilterConcepts(List<Concept> concepts, string paragraph, string paragraphType)
    {
        var max = paragraphType == "header" ? TextUtils.MaxHeader : TextUtils.MaxBody;
        return concepts
            .Select(c => NormalizeConcept(c, paragraph))
            .Where(c => c != null)
            .Select(c => c!)
            .DistinctBy(c => c.Label.ToLowerInvariant())
            .OrderByDescending(c => c.Label.Length)
            .Take(max)
            .ToList();
    }

    private static Concept? NormalizeConcept(Concept c, string paragraph)
    {
        if (string.IsNullOrWhiteSpace(c.Label)) return null;
        if (!AllowedCategories.Contains(c.Category)) return null;

        var forms = c.SurfaceForms
            .Where(f => f is { Length: > 0 and <= 100 }
                        && paragraph.Contains(f, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        return c with { SurfaceForms = forms };
    }

    private async Task Persist(ConceptsData data, Dictionary<int, ParagraphEntry> byIndex, string sourceHash, HashSet<int> currentIndices)
    {
        foreach (var key in byIndex.Keys.Where(k => !currentIndices.Contains(k)).ToList())
            byIndex.Remove(key);

        data.Paragraphs = byIndex.Values.OrderBy(p => p.Index).ToList();
        data.ParagraphCount = data.Paragraphs.Count;
        data.ConceptCount = data.Paragraphs.Sum(p => p.Concepts.Count);
        data.SourceHash = sourceHash;
        data.ConceptsHash = HashUtils.HashObject(
            data.Paragraphs.Select(p => new
            {
                p.Index, p.Hash,
                concepts = p.Concepts.Select(c => new { c.Label, c.Category, c.NeedsSearch, c.SearchQuery, c.SurfaceForms })
            }));
        await FileUtils.SafeWriteJson(cfg.ConceptsPath, data);
    }

    internal static List<ConceptEntry> BuildConceptEntries(ConceptsData data) =>
        data.Paragraphs
            .SelectMany(p => p.Concepts.Select(c => new ConceptEntry
            {
                Label = c.Label,
                Category = c.Category,
                NeedsSearch = c.NeedsSearch,
                SearchQuery = c.SearchQuery,
                Reason = c.Reason,
                SurfaceForms = c.SurfaceForms,
                ParagraphIndex = p.Index
            }))
            .Select((c, id) => c with { Id = id })
            .ToList();

    private record ExtractResult
    {
        public List<Concept>? Concepts { get; init; }
    }
}
