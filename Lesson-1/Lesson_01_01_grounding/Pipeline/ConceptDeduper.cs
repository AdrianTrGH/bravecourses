using Lesson_1.Lesson_01_01_grounding;
using Lesson_1.Lesson_01_01_grounding.Models;
using Lesson_1.Lesson_01_01_grounding.Prompts;
using Lesson_1.Lesson_01_01_grounding.Schemas;
using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Lesson_01_01_grounding.Pipeline;

internal class ConceptDeduper(ApiClient api, GroundingConfig cfg)
{
    internal async Task<DedupeData> DedupeConcepts(ConceptsData conceptsData)
    {
        var existing = FileUtils.ReadJsonIfExists<DedupeData>(cfg.DedupePath);
        var sameSource = existing != null && existing.SourceFile == conceptsData.SourceFile;
        var sameCounts = existing?.ParagraphCount == conceptsData.ParagraphCount
                         && existing?.ConceptCount == conceptsData.ConceptCount;
        var sameSourceHash = existing?.SourceHash == conceptsData.SourceHash;
        var sameConceptsHash = existing?.ConceptsHash == conceptsData.ConceptsHash;

        if (sameSource && sameCounts && sameSourceHash && sameConceptsHash && !cfg.Force)
        {
            Console.WriteLine("   Using cached dedupe data");
            if (string.IsNullOrEmpty(existing!.DedupeHash))
            {
                existing.DedupeHash = HashUtils.HashObject(existing.Groups);
                await FileUtils.SafeWriteJson(cfg.DedupePath, existing);
            }
            return existing;
        }

        var conceptEntries = ConceptExtractor.BuildConceptEntries(conceptsData)
            .Where(c => c.NeedsSearch)
            .ToList();

        if (conceptEntries.Count == 0)
        {
            var empty = new DedupeData
            {
                SourceFile = conceptsData.SourceFile,
                Model = cfg.Model,
                SourceHash = conceptsData.SourceHash,
                ConceptsHash = conceptsData.ConceptsHash,
                ParagraphCount = conceptsData.ParagraphCount,
                ConceptCount = conceptsData.ConceptCount,
                DedupeHash = HashUtils.HashObject(new object[0]),
                Groups = []
            };
            await FileUtils.SafeWriteJson(cfg.DedupePath, empty);
            return empty;
        }

        var input = PromptBuilders.BuildDedupePrompt(conceptEntries);
        var doc = await api.Chat(cfg.Model, input, JsonSchemas.Dedupe, reasoning: new { effort = "medium" });
        var result = ApiClient.ExtractJson<DedupeResult>(doc, "concept dedupe");

        var dedupeData = new DedupeData
        {
            SourceFile = conceptsData.SourceFile,
            Model = cfg.Model,
            SourceHash = conceptsData.SourceHash,
            ConceptsHash = conceptsData.ConceptsHash,
            ParagraphCount = conceptsData.ParagraphCount,
            ConceptCount = conceptsData.ConceptCount,
            DedupeHash = HashUtils.HashObject(result.Groups ?? []),
            Groups = result.Groups ?? []
        };

        await FileUtils.SafeWriteJson(cfg.DedupePath, dedupeData);
        return dedupeData;
    }

    private record DedupeResult
    {
        public List<DedupeGroup>? Groups { get; init; }
    }
}
