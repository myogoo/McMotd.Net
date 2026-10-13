using System.Text.Json;
using McMotdParser.Enum;

namespace McMotdParser.Test;

public class MotdCoverageTests
{
    private static string Plain(IEnumerable<MotdContent> contents) =>
        string.Concat(contents.Select(content => (content.LineBreak ? "\n" : "") + content.Text));

    [Theory]
    [InlineData("Hello \"World\"")]
    [InlineData(@"C:\games\new")]
    [InlineData("Hello§")]
    [InlineData("Hello§§")]
    [InlineData("§qHello")]
    [InlineData("{not json}")]
    [InlineData("[Welcome]")]
    [InlineData("한글 😀 <tag> & text")]
    public void PlainTextAndUnknownCodesArePreserved(string text)
    {
        Assert.Equal(text, Plain(new MotdParser().deserialize(text)));
    }

    [Theory]
    [InlineData("A\nB", "A\nB")]
    [InlineData("A\r\nB", "A\nB")]
    [InlineData("A\rB", "A\nB")]
    [InlineData("\nA\n\n", "\nA\n\n")]
    [InlineData("\n\n", "\n\n")]
    public void PlainAndJsonTextPreserveEveryLineBreak(string text, string expected)
    {
        var parser = new MotdParser();
        Assert.Equal(expected, Plain(parser.deserialize(text)));
        Assert.Equal(expected, Plain(parser.deserialize(JsonSerializer.Serialize(new { text }))));
    }

    [Fact]
    public void FormattingCodesRetainColorAndResetClearsFormatting()
    {
        var contents = new MotdParser().deserialize("§aGreen §lBold§rReset");
        Assert.Equal("#55FF55", contents[1].Color);
        Assert.Contains(TextFormatEnum.Bold, contents[1].TextFormatting);
        Assert.Equal("#808080", contents[2].Color);
        Assert.Empty(contents[2].TextFormatting);
    }

    [Theory]
    [InlineData("§#12abEFHello")]
    [InlineData("§x§1§2§a§b§E§FHello")]
    public void RgbLegacyColorsAreParsed(string raw)
    {
        var content = Assert.Single(new MotdParser().deserialize(raw));
        Assert.Equal("Hello", content.Text);
        Assert.Equal("#12ABEF", content.Color, ignoreCase: true);
    }

    [Fact]
    public void NestedComponentsInheritAndExplicitFalseDisablesFormatting()
    {
        const string raw = """
            {"text":"A","color":"red","bold":true,"extra":[
              {"text":"B","extra":[{"text":"C","bold":false,"italic":true}]},
              {"text":"D"}]}
            """;
        var contents = new MotdParser().deserialize(raw);
        Assert.Equal("ABCD", Plain(contents));
        Assert.All(contents, content => Assert.Equal("#FF5555", content.Color));
        Assert.DoesNotContain(TextFormatEnum.Bold, contents.Single(content => content.Text == "C").TextFormatting);
        Assert.Contains(TextFormatEnum.Italic, contents.Single(content => content.Text == "C").TextFormatting);
        Assert.Contains(TextFormatEnum.Bold, contents.Last().TextFormatting);
    }

    [Fact]
    public void ArraysAndPrimitiveComponentsAreAccepted()
    {
        Assert.Equal("AB42true", Plain(new MotdParser().deserialize("[\"A\",{\"text\":\"B\"},42,true]")));
    }

    [Fact]
    public void EmptyContainerAddsNoInventedSpaces()
    {
        Assert.Equal("AB", Plain(new MotdParser().deserialize("{\"text\":\"\",\"extra\":[\"A\",{\"text\":\"\"},\"B\"]}")));
    }

    [Fact]
    public void HtmlEncodesTextAndRejectsInjectedColors()
    {
        var raw = JsonSerializer.Serialize(new { text = "<script>alert('x')</script>&", color = "#fff\" onmouseover=\"alert(1)" });
        var html = new MotdParser().ToHtml(raw);
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("onmouseover", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp;", html);
    }

    [Fact]
    public void HtmlCombinesUnderlineAndStrikethrough()
    {
        var html = new MotdParser().ToHtml("{\"text\":\"Hello\",\"underlined\":true,\"strikethrough\":true}");
        Assert.Contains("text-decoration:underline line-through;", html);
    }
}
