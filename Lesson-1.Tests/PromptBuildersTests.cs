using System.Text.Json;
using Lesson_1.Lesson_01_01_grounding.Models;
using Lesson_1.Lesson_01_01_grounding.Prompts;

namespace Lesson_1.Tests;

public class PromptBuildersTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static string Serialize(object obj) => JsonSerializer.Serialize(obj);

    private record Message(string Role, string Content);

    private static List<Message> ToMessages(object prompt) =>
        JsonSerializer.Deserialize<List<Message>>(Serialize(prompt), JsonOpts)!;

    [Fact]
    public void BuildExtractPrompt_ContainsParagraphText()
    {
        var prompt = PromptBuilders.BuildExtractPrompt("Hello world", "body", "2-5", 0, 3);
        var messages = ToMessages(prompt);
        Assert.Contains(messages, m => m.Content.Contains("Hello world"));
    }

    [Fact]
    public void BuildExtractPrompt_ContainsParagraphIndex()
    {
        var prompt = PromptBuilders.BuildExtractPrompt("text", "body", "2-5", 2, 5);
        var messages = ToMessages(prompt);
        Assert.Contains(messages, m => m.Content.Contains("3/5"));
    }

    [Fact]
    public void BuildExtractPrompt_HasSystemAndUserMessages()
    {
        var prompt = PromptBuilders.BuildExtractPrompt("text", "header", "0-1", 0, 1);
        var messages = ToMessages(prompt);
        Assert.Contains(messages, m => m.Role == "system");
        Assert.Contains(messages, m => m.Role == "user");
    }

    [Fact]
    public void BuildDedupePrompt_ContainsConceptJson()
    {
        var entries = new List<ConceptEntry>
        {
            new() { Id = 0, Label = "transformer", Category = "term", NeedsSearch = true, SurfaceForms = ["transformer model"] }
        };
        var prompt = PromptBuilders.BuildDedupePrompt(entries);
        var messages = ToMessages(prompt);
        var content = messages[0].Content;
        Assert.Contains("transformer", content);
        Assert.Contains("<concepts>", content);
    }

    [Fact]
    public void BuildSearchPrompt_ContainsCanonical()
    {
        var prompt = PromptBuilders.BuildSearchPrompt("Proof of Work", "proof of work consensus", ["PoW"]);
        var messages = ToMessages(prompt);
        var content = messages[0].Content;
        Assert.Contains("Proof of Work", content);
        Assert.Contains("PoW", content);
        Assert.Contains("proof of work consensus", content);
    }

    [Fact]
    public void BuildSearchPrompt_NoAliases_DoesNotAddAliasLine()
    {
        var prompt = PromptBuilders.BuildSearchPrompt("Bitcoin", null, []);
        var messages = ToMessages(prompt);
        Assert.DoesNotContain("Also known as", messages[0].Content);
    }

    [Fact]
    public void BuildGroundPrompt_ContainsParagraphAndIndex()
    {
        var prompt = PromptBuilders.BuildGroundPrompt("The blockchain is immutable.", new object[0], 1, 4);
        var messages = ToMessages(prompt);
        var content = messages[0].Content;
        Assert.Contains("The blockchain is immutable.", content);
        Assert.Contains("2/4", content);
    }
}
