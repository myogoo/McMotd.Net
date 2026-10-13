using System.Xml.Linq;

namespace McMotdParser.Test;

public class MotdParserTest
{
    [Theory]
    [InlineData("기모찌서버", "기모찌서버")]
    [InlineData("  hello\t world  ", "  hello\t world  ")]
    [InlineData("<script>alert('x')</script>&", "<script>alert('x')</script>&")]
    [InlineData("{\"text\":\"Hello \",\"extra\":[{\"text\":\"World\"}]}", "Hello World")]
    public void HtmlPreservesAndEncodesText(string raw, string expected)
    {
        var document = XElement.Parse(new MotdParser().ToHtml(raw));
        Assert.Equal(expected, document.Value);
        Assert.Equal("mcmotd-container", (string?)document.Attribute("class"));
        Assert.Contains("white-space:pre-wrap", (string?)document.Attribute("style"));
        Assert.All(document.Elements(), child => Assert.Equal("span", child.Name.LocalName));
    }

    [Fact]
    public void HtmlPreservesLineBreaksIncludingEmptyLines()
    {
        var document = XElement.Parse(new MotdParser().ToHtml("A\r\n\r\nB\n"));
        Assert.Equal(3, document.Elements("br").Count());
        Assert.Equal("AB", document.Value);
    }

    [Fact]
    public void HtmlCombinesFormattingAndColor()
    {
        var document = XElement.Parse(new MotdParser().ToHtml("§b§l§o§n§mHello"));
        var span = Assert.Single(document.Elements("span"));
        var style = (string)span.Attribute("style")!;
        Assert.Contains("color:#55FFFF;", style);
        Assert.Contains("font-weight:bold;", style);
        Assert.Contains("font-style:italic;", style);
        Assert.Contains("text-decoration:underline line-through;", style);
    }
}
