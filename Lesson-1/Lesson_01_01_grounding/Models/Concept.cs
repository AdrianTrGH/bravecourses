namespace Lesson_1.Lesson_01_01_grounding.Models;

internal record Concept
{
    public string Label { get; init; } = "";
    public string Category { get; init; } = "";
    public bool NeedsSearch { get; init; }
    public string? SearchQuery { get; init; }
    public string? Reason { get; init; }
    public List<string> SurfaceForms { get; init; } = [];
}

internal record ConceptEntry : Concept
{
    public int Id { get; init; }
    public int ParagraphIndex { get; init; }
}
