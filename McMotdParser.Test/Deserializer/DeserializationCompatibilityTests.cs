using System.Text.Json;
using McMotdParser.Utils;

namespace McMotdParser.Test.Deserializer;

public class DeserializationCompatibilityTests
{
    [Theory]
    [InlineData("Hello")]
    [InlineData("\"Hello\"")]
    [InlineData("§aHello §lWorld")]
    [InlineData("First\r\nSecond")]
    [InlineData("{\"text\":\"Hello\",\"extra\":[{\"color\":\"red\",\"text\":\"World\"}]}")]
    [InlineData("{\"text\":\"First\\n\",\"extra\":[{\"text\":\"Second\"}]}")]
    public void DirectParsingMatchesThePublicWrapper(string rawMotd)
    {
        var wrapped = new StringUtilsBuilder()
            .setString(rawMotd)
            .QuotePlace()
            .EscapeCharacterReplace()
            .ToJsonObjectString()
            .build();
        var expected = JsonSerializer.Deserialize<MotdContents>(wrapped)!.Contents;

        Assert.Equal(expected, new MotdParser().deserialize(rawMotd));
    }

    [Fact]
    public void ReusedJsonOptionsDoNotCarryLineBreaksBetweenDocuments()
    {
        var options = new JsonSerializerOptions();
        JsonSerializer.Deserialize<MotdContents>("{\"Contents\":{\"text\":\"First\\n\"}}", options);

        var second = JsonSerializer.Deserialize<MotdContents>("{\"Contents\":{\"text\":\"Second\"}}", options)!;

        Assert.False(Assert.Single(second.Contents).LineBreak);
    }

    [Fact]
    public void ParserCallsDoNotCarryLineBreaksBetweenDocuments()
    {
        var parser = new MotdParser();
        parser.deserialize("{\"text\":\"First\\n\"}");

        Assert.False(Assert.Single(parser.deserialize("{\"text\":\"Second\"}")).LineBreak);
    }

    [Fact]
    public void ConcurrentCallsKeepLineBreaksWithinTheirOwnDocument()
    {
        var parser = new MotdParser();

        Parallel.For(0, 200, index =>
        {
            var contents = parser.deserialize("{\"text\":\"First\\n\",\"extra\":[{\"text\":\"Second\"}]}");

            Assert.Equal(2, contents.Count);
            Assert.False(contents[0].LineBreak);
            Assert.True(contents[1].LineBreak);
            Assert.False(Assert.Single(parser.deserialize("{\"text\":\"Independent\"}")).LineBreak);
        });
    }
}
