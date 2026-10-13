using McMotdParser.Enum;

namespace McMotdParser.Test;

public class MotdContentTests
{
    [Fact]
    public void DifferentFormattingWithTheSameCountIsNotEqual()
    {
        var bold = new MotdContent { Text = "Hello", TextFormatting = new() { TextFormatEnum.Bold } };
        var italic = new MotdContent { Text = "Hello", TextFormatting = new() { TextFormatEnum.Italic } };

        Assert.False(bold.Equals(italic));
    }

    [Fact]
    public void LineBreakIsPartOfEquality()
    {
        var inline = new MotdContent { Text = "Hello" };
        var newLine = new MotdContent { Text = "Hello", LineBreak = true };

        Assert.False(inline.Equals(newLine));
    }

    [Fact]
    public void FormattingOrderDoesNotAffectEqualityOrHashCode()
    {
        var first = new MotdContent
        {
            Text = "Hello",
            TextFormatting = new() { TextFormatEnum.Bold, TextFormatEnum.Italic }
        };
        var second = new MotdContent
        {
            Text = "Hello",
            TextFormatting = new() { TextFormatEnum.Italic, TextFormatEnum.Bold }
        };

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Contains(second, new HashSet<MotdContent> { first });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void DifferentContentCountsAreNotEqual(int firstCount, int secondCount)
    {
        var first = new MotdContents { Contents = Enumerable.Repeat(new MotdContent { Text = "Hello" }, firstCount).ToList() };
        var second = new MotdContents { Contents = Enumerable.Repeat(new MotdContent { Text = "Hello" }, secondCount).ToList() };

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void EqualContentsHaveEqualHashCodes()
    {
        var first = new MotdContents { Contents = new() { new MotdContent { Text = "Hello" } } };
        var second = new MotdContents { Contents = new() { new MotdContent { Text = "Hello" } } };

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Contains(second, new HashSet<MotdContents> { first });
    }
}
