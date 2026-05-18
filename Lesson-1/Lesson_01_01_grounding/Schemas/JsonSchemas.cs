namespace Lesson_1.Lesson_01_01_grounding.Schemas;

internal static class JsonSchemas
{
    internal static object Extract => new
    {
        type = "json_schema",
        name = "concept_extraction",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                concepts = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            label = new { type = "string", description = "Canonical name for the extracted claim or term." },
                            category = new { type = "string", @enum = new[] { "claim", "result", "method", "metric", "resource", "definition", "term", "entity", "reference" } },
                            needsSearch = new { type = "boolean" },
                            searchQuery = new { type = (object)new[] { "string", "null" } },
                            reason = new { type = "string" },
                            surfaceForms = new
                            {
                                type = "array",
                                items = new { type = "string" },
                                minItems = 1,
                                description = "Short key phrases (3-12 words) directly from source text."
                            }
                        },
                        required = new[] { "label", "category", "needsSearch", "searchQuery", "reason", "surfaceForms" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "concepts" },
            additionalProperties = false
        }
    };

    internal static object Dedupe => new
    {
        type = "json_schema",
        name = "concept_dedupe",
        strict = true,
        schema = new
        {
            type = "object",
            description = "Groups of equivalent or near-equivalent concepts.",
            properties = new
            {
                groups = new
                {
                    type = "array",
                    description = "Each group clusters concept ids that refer to the same idea.",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            canonical = new { type = "string", description = "Preferred canonical label for the group." },
                            ids = new { type = "array", items = new { type = "number" }, minItems = 1 },
                            aliases = new { type = "array", items = new { type = "string" } },
                            rationale = new { type = "string" }
                        },
                        required = new[] { "canonical", "ids", "aliases", "rationale" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "groups" },
            additionalProperties = false
        }
    };

    internal static object Search => new
    {
        type = "json_schema",
        name = "web_search_result",
        strict = true,
        schema = new
        {
            type = "object",
            description = "Web search summary and sources for a single concept.",
            properties = new
            {
                summary = new { type = "string", description = "Concise factual summary grounded in sources." },
                keyPoints = new { type = "array", items = new { type = "string" }, minItems = 0 },
                sources = new
                {
                    type = "array",
                    minItems = 0,
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            title = new { type = (object)new[] { "string", "null" } },
                            url = new { type = "string" }
                        },
                        required = new[] { "title", "url" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "summary", "keyPoints", "sources" },
            additionalProperties = false
        }
    };

    internal static object Ground => new
    {
        type = "json_schema",
        name = "grounded_paragraph",
        strict = true,
        schema = new
        {
            type = "object",
            description = "HTML output for a single grounded paragraph.",
            properties = new
            {
                html = new { type = "string", description = "HTML fragment for this paragraph with grounded spans." }
            },
            required = new[] { "html" },
            additionalProperties = false
        }
    };
}
