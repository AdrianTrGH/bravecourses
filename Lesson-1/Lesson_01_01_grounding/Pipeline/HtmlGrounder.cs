using Lesson_1.Lesson_01_01_grounding;
using System.Text;
using System.Text.Json;
using Lesson_1.Lesson_01_01_grounding.Models;
using Lesson_1.Lesson_01_01_grounding.Prompts;
using Lesson_1.Lesson_01_01_grounding.Schemas;
using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Lesson_01_01_grounding.Pipeline;

internal class HtmlGrounder(ApiClient api, GroundingConfig cfg)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal async Task GenerateAndApplyTemplate(
        List<string> paragraphs,
        ConceptsData conceptsData,
        DedupeData dedupeData,
        SearchData searchData)
    {
        if (File.Exists(cfg.GroundedPath) && !cfg.Force)
        {
            Console.WriteLine("   Output already exists, skipping (use --force to regenerate)");
            return;
        }

        if (!File.Exists(cfg.TemplatePath))
            throw new FileNotFoundException($"template.html not found at {cfg.TemplatePath}");

        var template = await File.ReadAllTextAsync(cfg.TemplatePath);
        if (!template.Contains("<!--CONTENT-->"))
            throw new Exception("template.html is missing <!--CONTENT--> placeholder");

        var groundingItems = BuildGroundingItems(conceptsData, dedupeData, searchData);
        var htmlParts = new List<string>();

        var batches = TextUtils.Chunk(paragraphs, cfg.BatchSize);
        Console.WriteLine($"  Grounding {paragraphs.Count} paragraphs ({cfg.BatchSize} parallel)");

        foreach (var (batch, batchIdx) in batches.Select((b, i) => (b, i)))
        {
            Console.WriteLine($"  [batch {batchIdx + 1}/{batches.Count}]");

            var batchOffset = batchIdx * cfg.BatchSize;
            var results = await Task.WhenAll(batch.Select((para, i) =>
                GroundSingle(para, batchOffset + i, paragraphs.Count, groundingItems)));

            htmlParts.AddRange(results);
        }

        var bodyHtml = string.Join("\n", htmlParts);
        var output = template.Replace("<!--CONTENT-->", bodyHtml);

        FileUtils.EnsureDir(cfg.OutputDir);
        await File.WriteAllTextAsync(cfg.GroundedPath, output);
        Console.WriteLine($"   Written: {cfg.GroundedPath}");
    }

    private async Task<string> GroundSingle(string paragraph, int index, int total, List<object> groundingItems)
    {
        var relevant = groundingItems
            .Where(item =>
            {
                if (item is not GroundingItem g) return false;
                return g.SurfaceForms.Any(sf =>
                    paragraph.Contains(sf, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        if (relevant.Count == 0)
            return ConvertToBasicHtml(paragraph);

        var input = PromptBuilders.BuildGroundPrompt(paragraph, relevant, index, total);
        try
        {
            var doc = await api.Chat(cfg.Model, input, JsonSchemas.Ground);
            var result = ApiClient.ExtractJson<GroundResult>(doc, $"ground paragraph {index + 1}");
            return result.Html ?? ConvertToBasicHtml(paragraph);
        }
        catch
        {
            return ConvertToBasicHtml(paragraph);
        }
    }

    private static List<object> BuildGroundingItems(ConceptsData conceptsData, DedupeData dedupeData, SearchData searchData)
    {
        var conceptEntries = ConceptExtractor.BuildConceptEntries(conceptsData)
            .ToDictionary(c => c.Id, c => c);

        return dedupeData.Groups
            .Where(g => searchData.ResultsByCanonical.ContainsKey(g.Canonical))
            .Select(g =>
            {
                var sr = searchData.ResultsByCanonical[g.Canonical];
                var members = g.Ids.Select(id => conceptEntries.TryGetValue(id, out var c) ? c : null)
                               .Where(c => c != null).Select(c => c!).ToList();
                var surfaceForms = members.SelectMany(m => m.SurfaceForms).Distinct().ToList();

                return (object)new GroundingItem(
                    g.Canonical,
                    surfaceForms,
                    sr.Summary,
                    sr.Sources.Select(s => new GroundingSource(s.Title, s.Url)).ToList()
                );
            })
            .ToList();
    }

    private static string ConvertToBasicHtml(string paragraph)
    {
        var headerMatch = System.Text.RegularExpressions.Regex.Match(paragraph, @"^(#{1,6})\s+(.+)");
        if (headerMatch.Success)
        {
            var level = headerMatch.Groups[1].Value.Length;
            var text = EscapeHtml(headerMatch.Groups[2].Value);
            return $"<h{level}>{text}</h{level}>";
        }

        if (paragraph.StartsWith("- ") || paragraph.StartsWith("* "))
        {
            var lines = paragraph.Split('\n').Where(l => l.TrimStart().StartsWith("- ") || l.TrimStart().StartsWith("* "));
            var items = string.Join("", lines.Select(l => $"<li>{EscapeHtml(l.TrimStart()[2..])}</li>"));
            return $"<ul>{items}</ul>";
        }

        return $"<p>{EscapeHtml(paragraph)}</p>";
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private record GroundingItem(string Canonical, List<string> SurfaceForms, string Summary, List<GroundingSource> Sources);
    private record GroundingSource(string? Title, string Url);
    private record GroundResult { public string? Html { get; init; } }
}
