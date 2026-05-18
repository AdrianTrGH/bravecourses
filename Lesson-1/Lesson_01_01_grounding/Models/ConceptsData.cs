namespace Lesson_1.Lesson_01_01_grounding.Models;

internal record ParagraphEntry
{
    public int Index { get; init; }
    public string Hash { get; init; } = "";
    public string Text { get; init; } = "";
    public List<Concept> Concepts { get; init; } = [];
}

internal class ConceptsData
{
    public string SourceFile { get; set; } = "";
    public string Model { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string ConceptsHash { get; set; } = "";
    public int ParagraphCount { get; set; }
    public int ConceptCount { get; set; }
    public List<ParagraphEntry> Paragraphs { get; set; } = [];
}
