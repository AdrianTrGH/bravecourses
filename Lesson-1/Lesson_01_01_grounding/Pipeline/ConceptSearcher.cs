using Lesson_1.Lesson_01_01_grounding;
using Lesson_1.Lesson_01_01_grounding.Models;
using Lesson_1.Lesson_01_01_grounding.Prompts;
using Lesson_1.Lesson_01_01_grounding.Schemas;
using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Lesson_01_01_grounding.Pipeline;

internal class ConceptSearcher(ApiClient api, GroundingConfig cfg)
{
    internal async Task<SearchData> SearchConcepts(ConceptsData conceptsData, DedupeData dedupeData)
    {
        Console.WriteLine($"   Using search model: {cfg.SearchModel}");

        var existing = FileUtils.ReadJsonIfExists<SearchData>(cfg.SearchPath);
        var shouldReuse = existing != null && existing.SourceFile == conceptsData.SourceFile && !cfg.Force;
        var sameSourceHash = existing?.SourceHash == conceptsData.SourceHash;
        var sameDedupeHash = existing?.DedupeHash == dedupeData.DedupeHash;
        var sameModel = existing?.Model == cfg.SearchModel;

        var shouldReset = !sameSourceHash || !sameDedupeHash || !sameModel;
        if (shouldReuse && shouldReset)
            Console.WriteLine("   Search cache invalidated (source, dedupe, or model changed)");

        var base_ = shouldReuse && !shouldReset
            ? existing!
            : new SearchData
            {
                SourceFile = conceptsData.SourceFile,
                Model = cfg.SearchModel,
                SourceHash = conceptsData.SourceHash,
                DedupeHash = dedupeData.DedupeHash,
                ResultsByCanonical = []
            };

        var conceptEntries = ConceptExtractor.BuildConceptEntries(conceptsData)
            .Where(c => c.NeedsSearch)
            .ToList();
        var conceptById = conceptEntries.ToDictionary(c => c.Id, c => c);

        var canonicalConcepts = dedupeData.Groups.Select(group =>
        {
            var members = group.Ids.Select(id => conceptById.TryGetValue(id, out var c) ? c : null)
                              .Where(c => c != null).Select(c => c!).ToList();
            var searchQuery = members.FirstOrDefault(m => m.SearchQuery != null)?.SearchQuery ?? group.Canonical;
            var surfaceForms = members.SelectMany(m => m.SurfaceForms).Distinct().ToList();
            return (group.Canonical, group.Aliases, searchQuery, surfaceForms);
        }).ToList();

        var pending = canonicalConcepts
            .Where(c => !base_.ResultsByCanonical.ContainsKey(c.Canonical))
            .ToList();

        if (pending.Count == 0 && sameSourceHash && sameDedupeHash)
        {
            Console.WriteLine("   Using cached search results");
            return base_;
        }

        Console.WriteLine($"   {pending.Count} concepts to search ({cfg.BatchSize} parallel)");
        var batches = TextUtils.Chunk(pending, cfg.BatchSize);

        foreach (var (batch, batchIdx) in batches.Select((b, i) => (b, i)))
        {
            if (batch.Count == 0) continue;
            Console.WriteLine($"  [batch {batchIdx + 1}/{batches.Count}] Searching: {string.Join(", ", batch.Select(c => c.Canonical))}");

            var results = await Task.WhenAll(batch.Select(c => SearchSingle(c.Canonical, c.searchQuery, c.Aliases)));

            foreach (var result in results)
            {
                base_.ResultsByCanonical[result.Canonical] = result;
                Console.WriteLine($"    ✓ {result.Canonical} ({result.Sources.Count} sources)");
            }

            await FileUtils.SafeWriteJson(cfg.SearchPath, base_);
        }

        return base_;
    }

    private async Task<SearchResult> SearchSingle(string canonical, string searchQuery, List<string> aliases)
    {
        var input = PromptBuilders.BuildSearchPrompt(canonical, searchQuery, aliases);
        var doc = await api.Chat(cfg.SearchModel, input, JsonSchemas.Search);
        var result = ApiClient.ExtractJson<SearchApiResult>(doc, $"search: {canonical}");
        var rawSources = ApiClient.ExtractWebSources(doc);

        var sources = (result.Sources ?? [])
            .Select(s => new SearchSource { Title = s.Title, Url = s.Url })
            .ToList();

        if (sources.Count == 0)
            sources = rawSources.Select(s => new SearchSource { Title = s.Title, Url = s.Url }).ToList();

        return new SearchResult
        {
            Canonical = canonical,
            Summary = result.Summary ?? "",
            KeyPoints = result.KeyPoints ?? [],
            Sources = sources
        };
    }

    private record SearchApiResult
    {
        public string? Summary { get; init; }
        public List<string>? KeyPoints { get; init; }
        public List<SearchSource>? Sources { get; init; }
    }
}
