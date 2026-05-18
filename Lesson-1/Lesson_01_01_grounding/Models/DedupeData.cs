namespace Lesson_1.Lesson_01_01_grounding.Models;

internal record DedupeGroup
{
    public string Canonical { get; init; } = "";
    public List<int> Ids { get; init; } = [];
    public List<string> Aliases { get; init; } = [];
    public string Rationale { get; init; } = "";
}

internal class DedupeData
{
    public string SourceFile { get; set; } = "";
    public string Model { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string ConceptsHash { get; set; } = "";
    public int ParagraphCount { get; set; }
    public int ConceptCount { get; set; }
    public string DedupeHash { get; set; } = "";
    public List<DedupeGroup> Groups { get; set; } = [];
}
