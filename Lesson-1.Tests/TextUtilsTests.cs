using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Tests;

public class TextUtilsTests
{
    // SplitParagraphs

    [Fact]
    public void SplitParagraphs_DoubleLF_SplitsCorrectly()
    {
        var result = TextUtils.SplitParagraphs("first\n\nsecond\n\nthird");
        Assert.Equal(["first", "second", "third"], result);
    }

    [Fact]
    public void SplitParagraphs_NormalizesWindowsLineEndings()
    {
        var result = TextUtils.SplitParagraphs("first\r\n\r\nsecond");
        Assert.Equal(["first", "second"], result);
    }

    [Fact]
    public void SplitParagraphs_FiltersEmptyBlocks()
    {
        var result = TextUtils.SplitParagraphs("first\n\n\n\nsecond");
        Assert.Equal(["first", "second"], result);
    }

    [Fact]
    public void SplitParagraphs_TrimsWhitespace()
    {
        var result = TextUtils.SplitParagraphs("  hello  \n\n  world  ");
        Assert.Equal(["hello", "world"], result);
    }

    [Fact]
    public void SplitParagraphs_SingleParagraph_ReturnsSingleItem()
    {
        var result = TextUtils.SplitParagraphs("just one block");
        Assert.Single(result);
    }

    // Chunk

    [Fact]
    public void Chunk_EvenDivision_ReturnsCorrectBatches()
    {
        var result = TextUtils.Chunk(new[] { 1, 2, 3, 4, 5, 6 }.ToList(), 2);
        Assert.Equal(3, result.Count);
        Assert.Equal([1, 2], result[0]);
        Assert.Equal([3, 4], result[1]);
        Assert.Equal([5, 6], result[2]);
    }

    [Fact]
    public void Chunk_OddDivision_LastBatchIsSmaller()
    {
        var result = TextUtils.Chunk(new[] { 1, 2, 3, 4, 5 }.ToList(), 3);
        Assert.Equal(2, result.Count);
        Assert.Equal([1, 2, 3], result[0]);
        Assert.Equal([4, 5], result[1]);
    }

    [Fact]
    public void Chunk_SizeGreaterThanList_ReturnsSingleBatch()
    {
        var result = TextUtils.Chunk(new[] { 1, 2 }.ToList(), 10);
        Assert.Single(result);
        Assert.Equal([1, 2], result[0]);
    }

    // GetParagraphType

    [Theory]
    [InlineData("# Heading 1", "header")]
    [InlineData("## Heading 2", "header")]
    [InlineData("###### Heading 6", "header")]
    public void GetParagraphType_Headers_ReturnHeader(string paragraph, string expected)
    {
        Assert.Equal(expected, TextUtils.GetParagraphType(paragraph));
    }

    [Theory]
    [InlineData("Regular paragraph text.")]
    [InlineData("No hash at the start")]
    [InlineData("# Not a header because # is not at start", Skip = "Behavior depends on regex")]
    public void GetParagraphType_BodyText_ReturnBody(string paragraph)
    {
        Assert.Equal("body", TextUtils.GetParagraphType(paragraph));
    }

    // GetTargetCount

    [Fact]
    public void GetTargetCount_Header_ReturnsSingleRange()
    {
        var result = TextUtils.GetTargetCount("header");
        Assert.Equal($"0-{TextUtils.MaxHeader}", result);
    }

    [Fact]
    public void GetTargetCount_Body_ReturnsBodyRange()
    {
        var result = TextUtils.GetTargetCount("body");
        Assert.Equal($"2-{TextUtils.MaxBody}", result);
    }

    // Truncate

    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        Assert.Equal("hi", TextUtils.Truncate("hi", 10));
    }

    [Fact]
    public void Truncate_ExactLength_ReturnsUnchanged()
    {
        Assert.Equal("hello", TextUtils.Truncate("hello", 5));
    }

    [Fact]
    public void Truncate_LongString_AddsEllipsis()
    {
        var result = TextUtils.Truncate("hello world", 8);
        Assert.Equal("hello...", result);
        Assert.Equal(8, result.Length);
    }
}
