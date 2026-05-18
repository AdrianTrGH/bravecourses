namespace Lesson_1.Lesson_01_01_grounding.Utils;

internal static class TextUtils
{
    internal const int MaxBody = 5;
    internal const int MaxHeader = 1;

    internal static List<string> SplitParagraphs(string markdown) =>
        markdown
            .Replace("\r\n", "\n")
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .ToList();

    internal static List<List<T>> Chunk<T>(IList<T> items, int size)
    {
        var count = (int)Math.Ceiling(items.Count / (double)size);
        return Enumerable.Range(0, count)
            .Select(i => items.Skip(i * size).Take(size).ToList())
            .ToList();
    }

    internal static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 3)] + "...";

    internal static string GetParagraphType(string paragraph) =>
        System.Text.RegularExpressions.Regex.IsMatch(paragraph, @"^#{1,6}\s+") ? "header" : "body";

    internal static string GetTargetCount(string paragraphType) =>
        paragraphType == "header" ? $"0-{MaxHeader}" : $"2-{MaxBody}";
}
