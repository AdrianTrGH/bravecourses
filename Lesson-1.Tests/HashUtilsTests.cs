using Lesson_1.Lesson_01_01_grounding.Utils;

namespace Lesson_1.Tests;

public class HashUtilsTests
{
    [Fact]
    public void HashText_SameInput_ReturnsSameHash()
    {
        var h1 = HashUtils.HashText("hello world");
        var h2 = HashUtils.HashText("hello world");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashText_DifferentInput_ReturnsDifferentHash()
    {
        var h1 = HashUtils.HashText("hello");
        var h2 = HashUtils.HashText("world");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void HashText_ReturnsLowercaseHex()
    {
        var hash = HashUtils.HashText("test");
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashText_EmptyString_ReturnsKnownHash()
    {
        // SHA256("") is well-known
        var hash = HashUtils.HashText("");
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash);
    }

    [Fact]
    public void HashObject_SameStructure_ReturnsSameHash()
    {
        var h1 = HashUtils.HashObject(new { a = 1, b = "x" });
        var h2 = HashUtils.HashObject(new { a = 1, b = "x" });
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashObject_DifferentValues_ReturnsDifferentHash()
    {
        var h1 = HashUtils.HashObject(new { a = 1 });
        var h2 = HashUtils.HashObject(new { a = 2 });
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void HashObject_EmptyArray_IsStable()
    {
        var h1 = HashUtils.HashObject(Array.Empty<object>());
        var h2 = HashUtils.HashObject(Array.Empty<object>());
        Assert.Equal(h1, h2);
    }
}
