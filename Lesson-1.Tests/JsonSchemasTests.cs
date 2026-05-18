using System.Text.Json;
using Lesson_1.Lesson_01_01_grounding.Schemas;

namespace Lesson_1.Tests;

public class JsonSchemasTests
{
    private static JsonElement ToElement(object schema) =>
        JsonDocument.Parse(JsonSerializer.Serialize(schema)).RootElement;

    [Theory]
    [MemberData(nameof(AllSchemas))]
    public void Schema_HasTypeJsonSchema(string _, object schema)
    {
        var el = ToElement(schema);
        Assert.Equal("json_schema", el.GetProperty("type").GetString());
    }

    [Theory]
    [MemberData(nameof(AllSchemas))]
    public void Schema_IsStrict(string _, object schema)
    {
        var el = ToElement(schema);
        Assert.True(el.GetProperty("strict").GetBoolean());
    }

    [Theory]
    [MemberData(nameof(AllSchemas))]
    public void Schema_HasNestedSchema(string _, object schema)
    {
        var el = ToElement(schema);
        Assert.Equal(JsonValueKind.Object, el.GetProperty("schema").ValueKind);
    }

    [Theory]
    [MemberData(nameof(AllSchemas))]
    public void Schema_SerializesToValidJson(string _, object schema)
    {
        var json = JsonSerializer.Serialize(schema);
        var ex = Record.Exception(() => JsonDocument.Parse(json));
        Assert.Null(ex);
    }

    [Fact]
    public void ExtractSchema_HasConceptsArray()
    {
        var el = ToElement(JsonSchemas.Extract);
        var props = el.GetProperty("schema").GetProperty("properties");
        Assert.Equal(JsonValueKind.Object, props.GetProperty("concepts").ValueKind);
        Assert.Equal("array", props.GetProperty("concepts").GetProperty("type").GetString());
    }

    [Fact]
    public void DedupeSchema_HasGroupsArray()
    {
        var el = ToElement(JsonSchemas.Dedupe);
        var props = el.GetProperty("schema").GetProperty("properties");
        Assert.Equal("array", props.GetProperty("groups").GetProperty("type").GetString());
    }

    [Fact]
    public void SearchSchema_HasSummaryKeyPointsSources()
    {
        var el = ToElement(JsonSchemas.Search);
        var props = el.GetProperty("schema").GetProperty("properties");
        Assert.True(props.TryGetProperty("summary", out _));
        Assert.True(props.TryGetProperty("keyPoints", out _));
        Assert.True(props.TryGetProperty("sources", out _));
    }

    [Fact]
    public void GroundSchema_HasHtmlField()
    {
        var el = ToElement(JsonSchemas.Ground);
        var props = el.GetProperty("schema").GetProperty("properties");
        Assert.Equal("string", props.GetProperty("html").GetProperty("type").GetString());
    }

    public static TheoryData<string, object> AllSchemas => new()
    {
        { "extract", JsonSchemas.Extract },
        { "dedupe",  JsonSchemas.Dedupe  },
        { "search",  JsonSchemas.Search  },
        { "ground",  JsonSchemas.Ground  },
    };
}
