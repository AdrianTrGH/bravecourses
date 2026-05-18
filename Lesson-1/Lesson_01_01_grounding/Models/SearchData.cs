namespace Lesson_1.Lesson_01_01_grounding.Models;

internal record SearchSource
{
    public string? Title { get; init; }
    public string Url { get; init; } = "";
}

internal record SearchResult
{
    public string Canonical { get; init; } = "";
    public string Summary { get; init; } = "";
    public List<string> KeyPoints { get; init; } = [];
    public List<SearchSource> Sources { get; init; } = [];
}

internal class SearchData
{
    public string SourceFile { get; set; } = "";
    public string Model { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string DedupeHash { get; set; } = "";
    public Dictionary<string, SearchResult> ResultsByCanonical { get; set; } = [];
}
