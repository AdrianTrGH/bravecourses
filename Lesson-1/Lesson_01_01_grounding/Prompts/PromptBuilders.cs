using System.Text.Json;
using Lesson_1.Lesson_01_01_grounding.Models;

namespace Lesson_1.Lesson_01_01_grounding.Prompts;

internal static class PromptBuilders
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private const string ExtractionGuidelines = """
        GOAL: Extract verifiable claims and key terms that would benefit from web search grounding.

        CATEGORIES:
        - claim: A factual assertion that can be verified (e.g. "Transformers were introduced in 2017")
        - definition: An explanation of what something is
        - term: A technical term or concept name
        - entity: A named person, organization, product, or place
        - reference: A cited work, paper, or external resource
        - result: A measured outcome or finding (e.g. "99.95% energy reduction")
        - method: A technique, algorithm, or approach
        - metric: A quantitative measure or benchmark
        - resource: A dataset, tool, or system mentioned

        SURFACE FORMS: Short key phrases (3-12 words) that appear verbatim in the text.
        - Must be findable in the source paragraph
        - No markdown syntax
        - No full sentences

        RULES:
        - Extract only concepts worth verifying via web search
        - Avoid overlap between concepts
        - Avoid generic statements without factual content
        """;

    internal static object BuildExtractPrompt(string paragraph, string paragraphType, string targetCount, int index, int total)
    {
        var system = $"""
            {ExtractionGuidelines}
            Extract {targetCount} concepts from this {paragraphType} paragraph.
            Return JSON only.
            """;
        var user = $"[Paragraph {index + 1}/{total}]\n\n{paragraph}";
        return new[]
        {
            new { role = "system", content = system },
            new { role = "user", content = user }
        };
    }

    internal static object BuildDedupePrompt(IEnumerable<ConceptEntry> conceptEntries)
    {
        var json = JsonSerializer.Serialize(conceptEntries, JsonOpts);
        var text = $"""
            Group concepts only when they are strict paraphrases of the same claim or term.
            Do NOT group related-but-distinct ideas (cause/effect, property vs consequence, part/whole, example vs category, metric vs definition).
            Only group items with the same category; if categories differ, keep them separate even if similar.
            Every id must appear in exactly one group.
            Pick a concise canonical label that preserves the full meaning.
            aliases must be full alternative labels, not fragments or partial phrases.
            If unsure, do not group.
            Return JSON only.
            <concepts>
            {json}
            </concepts>
            """;
        return new[] { new { role = "user", content = text } };
    }

    internal static object BuildSearchPrompt(string canonical, string? searchQuery, List<string>? aliases)
    {
        var aliasLine = aliases?.Count > 0 ? $"\nAlso known as: {string.Join(", ", aliases)}" : "";
        var queryLine = searchQuery != null ? $"\nSearch query: {searchQuery}" : "";
        var text = $"""
            Use web search to verify and expand on this concept.
            Search thoroughly and provide accurate, factual information.
            Return JSON only, matching the schema.

            Requirements:
            - Write a concise summary grounded in search results
            - Include 2-4 key points with specific facts
            - List sources with titles and URLs from the search

            Concept: {canonical}{queryLine}{aliasLine}
            """;
        return new[] { new { role = "user", content = text } };
    }

    internal static object BuildGroundPrompt(string paragraph, IEnumerable<object> groundingItems, int index, int total)
    {
        var itemsJson = JsonSerializer.Serialize(groundingItems, JsonOpts);
        var text = $$"""
            Convert this paragraph to semantic HTML with grounded concepts.
            Wrap exact surfaceForms with <span class="grounded" data-grounding="...">phrase</span>
            where data-grounding is a JSON string: {"canonical":"...","summary":"...","sources":[{"title":"...","url":"..."}]}

            Rules:
            - Only wrap phrases appearing verbatim in source text
            - Use longest matching surfaceForm when overlaps occur
            - Avoid duplicate wrapping of same concept
            - Preserve original wording, do not add facts
            - Apply appropriate HTML semantic tags (p, h1-h6, ul, li, strong, em, code)
            - Return JSON only

            [Paragraph {{index + 1}}/{{total}}]
            <paragraph>
            {{paragraph}}
            </paragraph>
            <grounding_items>
            {{itemsJson}}
            </grounding_items>
            """;
        return new[] { new { role = "user", content = text } };
    }
}
