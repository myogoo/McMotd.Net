using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using McMotdParser.Enum;
using McMotdParser.Utils;

namespace McMotdParser.Test;

public class MotdFormatTests
{
    private static string Plain(IEnumerable<MotdContent> contents) =>
        string.Concat(contents.Select(content => (content.LineBreak ? "\n" : "") + content.Text));

    [Theory]
    [InlineData("0", "black", "#000000")]
    [InlineData("1", "dark_blue", "#0000AA")]
    [InlineData("2", "dark_green", "#00AA00")]
    [InlineData("3", "dark_aqua", "#00AAAA")]
    [InlineData("4", "dark_red", "#AA0000")]
    [InlineData("5", "dark_purple", "#AA00AA")]
    [InlineData("6", "gold", "#FFAA00")]
    [InlineData("7", "gray", "#AAAAAA")]
    [InlineData("8", "dark_gray", "#555555")]
    [InlineData("9", "blue", "#5555FF")]
    [InlineData("a", "green", "#55FF55")]
    [InlineData("b", "aqua", "#55FFFF")]
    [InlineData("c", "red", "#FF5555")]
    [InlineData("d", "light_purple", "#FF55FF")]
    [InlineData("e", "yellow", "#FFFF55")]
    [InlineData("f", "white", "#FFFFFF")]
    public void StandardColorsMatchInLegacyAndJson(string code, string name, string expected)
    {
        foreach (var edition in new[] { MinecraftEdition.Java, MinecraftEdition.Bedrock })
        {
            var parser = new MotdParser(new MotdParserOptions { Edition = edition });
            Assert.Equal(expected, Assert.Single(parser.deserialize("§" + code.ToUpperInvariant() + "Text")).Color);
            Assert.Equal(expected, Assert.Single(parser.deserialize(JsonSerializer.Serialize(new { text = "Text", color = name }))).Color);
        }
    }

    [Theory]
    [InlineData('g', "#DDD605")]
    [InlineData('h', "#E3D4D1")]
    [InlineData('i', "#CECACA")]
    [InlineData('j', "#443A3B")]
    [InlineData('m', "#971607")]
    [InlineData('n', "#B4684D")]
    [InlineData('p', "#DEB12D")]
    [InlineData('q', "#47A036")]
    [InlineData('s', "#2CBAA8")]
    [InlineData('t', "#21497B")]
    [InlineData('u', "#9A5CC6")]
    [InlineData('v', "#EB7114")]
    [InlineData('w', "#8BB3FF")]
    public void BedrockMaterialAndPartyColorsAreDistinctFromJava(char code, string expected)
    {
        var parser = new MotdParser(new MotdParserOptions { Edition = MinecraftEdition.Bedrock });
        var content = Assert.Single(parser.deserialize("§" + code + "Text"));
        Assert.Equal(expected, content.Color);
        Assert.Empty(content.TextFormatting);
        Assert.Equal("Text", content.Text);
    }

    [Theory]
    [InlineData('k', TextFormatEnum.Obfuscated)]
    [InlineData('l', TextFormatEnum.Bold)]
    [InlineData('m', TextFormatEnum.Striktethrough)]
    [InlineData('n', TextFormatEnum.Underline)]
    [InlineData('o', TextFormatEnum.Italic)]
    public void AllJavaDecorationsAndUppercaseCodesAreSupported(char code, TextFormatEnum expected)
    {
        var content = Assert.Single(new MotdParser().deserialize("§" + char.ToUpperInvariant(code) + "Text"));
        Assert.Equal(new[] { expected }, content.TextFormatting);
    }

    [Fact]
    public void ApplyingAColorResetsJavaFormattingButRetainsBedrockFormatting()
    {
        var java = Assert.Single(new MotdParser().deserialize("§l§aText"));
        var bedrock = Assert.Single(new MotdParser(new MotdParserOptions { Edition = MinecraftEdition.Bedrock }).deserialize("§l§aText"));
        Assert.Empty(java.TextFormatting);
        Assert.Contains(TextFormatEnum.Bold, bedrock.TextFormatting);
    }

    [Fact]
    public void EscapeAndAmpersandInterpretationAreExplicit()
    {
        const string raw = @"\u00a7aHello\n&lWorld";
        Assert.Equal(raw, Plain(new MotdParser().deserialize(raw)));
        var parser = new MotdParser(new MotdParserOptions { InterpretEscapeSequences = true, LegacyCodeCharacter = '&' });
        var contents = parser.deserialize(raw);
        Assert.Equal("Hello\nWorld", Plain(contents));
        Assert.Equal("#55FF55", contents[1].Color);
        Assert.Contains(TextFormatEnum.Bold, contents[1].TextFormatting);
        Assert.Equal(@"\n", StringUtils.DecodeEscapes(@"\\n"));
        Assert.Equal(@"\uNOPE", StringUtils.DecodeEscapes(@"\uNOPE"));
    }

    [Fact]
    public void LegacyPublicBuilderStillProducesValidJson()
    {
        const string raw = "Hello \"World\"\nC:\\games";
        var wrapper = new StringUtilsBuilder().setString(raw).QuotePlace().EscapeCharacterReplace().ToJsonObjectString().build();
        Assert.Equal(raw, Plain(JsonSerializer.Deserialize<MotdContents>(wrapper)!.Contents));
        Assert.Equal(raw, StringUtils.QuotesREMOVE(StringUtils.QuotesPlace(raw)));
    }

    [Fact]
    public void ArraysUseTheFirstComponentsStyleForLaterMembers()
    {
        var contents = new MotdParser().deserialize("[{\"text\":\"A\",\"color\":\"red\"},\"B\",{\"text\":\"C\",\"color\":\"blue\"},\"D\"]");
        Assert.Equal(new[] { "#FF5555", "#FF5555", "#5555FF", "#FF5555" }, contents.Select(content => content.Color));
    }

    [Fact]
    public void BedrockRawtextCarriesFormattingAcrossNodes()
    {
        var parser = new MotdParser(new MotdParserOptions { Edition = MinecraftEdition.Bedrock });
        var contents = parser.deserialize("{\"rawtext\":[{\"text\":\"§aA\"},{\"text\":\"§lB\"},{\"text\":\"§rC\"}]}");
        Assert.Equal("ABC", Plain(contents));
        Assert.Equal("#55FF55", contents[1].Color);
        Assert.Contains(TextFormatEnum.Bold, contents[1].TextFormatting);
        Assert.Empty(contents[2].TextFormatting);
    }

    [Fact]
    public void TranslationsResolveFallbackAndStyledPositionalArguments()
    {
        var parser = new MotdParser(new MotdParserOptions { TranslationResolver = key => key == "known" ? "[%2$s] %s %%" : null });
        const string raw = "{\"translate\":\"known\",\"color\":\"red\",\"with\":[\"A\",{\"text\":\"B\",\"bold\":true}]}";
        var contents = parser.deserialize(raw);
        Assert.Equal("[B] A %", Plain(contents));
        Assert.Contains(TextFormatEnum.Bold, contents.Single(content => content.Text == "B").TextFormatting);
        Assert.All(contents, content => Assert.Equal("#FF5555", content.Color));
        Assert.Equal("fallback A", Plain(parser.deserialize("{\"translate\":\"missing\",\"fallback\":\"fallback %s\",\"with\":[\"A\"]}")));
        Assert.Equal("missing", Plain(parser.deserialize("{\"translate\":\"missing\"}")));
    }

    [Fact]
    public void BedrockTranslationsSupportRawtextArgumentsAndNumberedSlots()
    {
        var parser = new MotdParser(new MotdParserOptions { Edition = MinecraftEdition.Bedrock });
        var contents = parser.deserialize("{\"rawtext\":[{\"translate\":\"%%2 %s %%1 %%3\",\"with\":{\"rawtext\":[{\"text\":\"A\"},{\"text\":\"B\"}]}}]}");
        Assert.Equal("B A A ", Plain(contents));
    }

    [Theory]
    [InlineData("{\"rawtext\":[{\"translate\":\"§aA\"},{\"text\":\"B\"}]}")]
    [InlineData("{\"rawtext\":[{\"translate\":\"%%1B\",\"with\":[\"§aA\"]}]}")]
    [InlineData("{\"rawtext\":[{\"score\":{\"value\":\"§aA\"}},{\"text\":\"B\"}]}")]
    public void BedrockResolvedTextCarriesFormattingIntoFollowingText(string raw)
    {
        var parser = new MotdParser(new MotdParserOptions { Edition = MinecraftEdition.Bedrock });
        var contents = parser.deserialize(raw);
        Assert.Equal("AB", Plain(contents));
        Assert.All(contents, content => Assert.Equal("#55FF55", content.Color));
    }

    [Theory]
    [InlineData("{\"keybind\":\"key.jump\"}", "key.jump")]
    [InlineData("{\"selector\":\"@a\"}", "@a")]
    [InlineData("{\"score\":{\"name\":\"Player\",\"objective\":\"points\",\"value\":12}}", "12")]
    [InlineData("{\"score\":{\"name\":\"Player\",\"objective\":\"points\"}}", "[score:Player/points]")]
    [InlineData("{\"nbt\":\"CustomName\",\"entity\":\"@s\"}", "[nbt:CustomName]")]
    [InlineData("{\"object\":\"atlas\",\"sprite\":\"minecraft:item/emerald\"}", "[sprite:minecraft:item/emerald]")]
    [InlineData("{\"object\":\"player\",\"player\":{\"name\":\"Alex\"}}", "[player:Alex]")]
    public void ContextDependentComponentsKeepSourceAndReadableFallback(string raw, string fallback)
    {
        var content = Assert.Single(new MotdParser().deserialize(raw));
        Assert.Equal(fallback, content.Text);
        Assert.Equal(raw, content.Component!.Value.GetRawText());
        var parser = new MotdParser(new MotdParserOptions { ComponentResolver = _ => "resolved" });
        Assert.Equal("resolved", Assert.Single(parser.deserialize(raw)).Text);
    }

    [Fact]
    public void FullJavaStatusAndJsonElementInputsWork()
    {
        using var document = JsonDocument.Parse("{\"version\":{\"name\":\"test\"},\"description\":{\"text\":\"§aHello\"}}");
        var parser = new MotdParser();
        Assert.Equal("Hello", Plain(parser.deserialize(document.RootElement)));
        Assert.Equal("#55FF55", Assert.Single(parser.deserialize(document.RootElement)).Color);
    }

    [Fact]
    public void FontAndShadowAreInheritedWithoutCssInjection()
    {
        var parser = new MotdParser();
        var raw = JsonSerializer.Serialize(new { text = "A", font = "x\" onclick=\"y", shadow_color = new[] { 1.0, 0.0, 0.0, 0.5 }, extra = new[] { "B" } });
        var contents = parser.deserialize(raw);
        Assert.All(contents, content => Assert.Equal(0x80FF0000u, content.ShadowColor));
        var html = XElement.Parse(parser.RenderHtml(contents));
        Assert.All(html.Elements("span"), span =>
        {
            Assert.Null(span.Attribute("onclick"));
            Assert.Equal("x\" onclick=\"y", (string?)span.Attribute("data-mcmotd-font"));
            Assert.Contains("rgba(255,0,0,0.502)", (string?)span.Attribute("style"));
        });
    }

    [Fact]
    public void ObfuscationPreservesSourceGraphemesWhitespaceAndAccessibleText()
    {
        var parser = new MotdParser();
        var content = Assert.Single(parser.deserialize("§kA 😀 e\u0301"));
        var displayed = content.GetDisplayText();
        Assert.Equal(5, StringInfo.ParseCombiningCharacters(displayed).Length);
        Assert.Equal(' ', displayed[1]);
        Assert.Equal(' ', displayed[3]);
        Assert.Equal("A 😀 e\u0301", content.Text);
        var span = Assert.Single(XElement.Parse(parser.RenderHtml(new[] { content })).Elements("span"));
        Assert.Equal(content.Text, (string?)span.Attribute("aria-label"));
        Assert.Equal("img", (string?)span.Attribute("role"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"text\":null,\"extra\":[null]}")]
    [InlineData("{\"text\":123,\"bold\":{},\"color\":[]}")]
    [InlineData("{\"extra\":[{\"text\":\"A\"},true,{},42]}")]
    [InlineData("{\"translate\":\"%99999999999999999999$s\",\"with\":[\"A\"]}")]
    public void UnexpectedOptionalValuesDoNotCrash(string raw)
    {
        Assert.NotNull(new MotdParser().ToHtml(raw));
    }

    [Fact]
    public void CrLfAcrossJsonNodesIsOnlyOneLineBreak()
    {
        Assert.Equal("A\nB", Plain(new MotdParser().deserialize("{\"text\":\"A\\r\",\"extra\":[\"\\nB\"]}")));
    }

    [Fact]
    public void ConvertedContentsCanBeSerializedAndReadAgain()
    {
        var parser = new MotdParser();
        var original = new MotdContents { Contents = parser.deserialize("§a§lA\n§bB") };
        var json = JsonSerializer.Serialize(original);
        Assert.Equal(original.Contents, JsonSerializer.Deserialize<MotdContents>(json)!.Contents);
    }

    [Fact]
    public void InputAndExpansionLimitsPreventUnboundedWork()
    {
        Assert.Throws<ArgumentException>(() => new MotdParser().deserialize(new string('x', 262145)));
        Assert.Throws<ArgumentNullException>(() => new MotdParser().deserialize((string)null!));
        var parser = new MotdParser(new MotdParserOptions { TranslationResolver = _ => new string('x', 262145) });
        Assert.Throws<ArgumentException>(() => parser.deserialize("{\"translate\":\"key\"}"));
    }

    [Fact]
    public void RepeatedEmptyTranslationArgumentsAreAlsoBounded()
    {
        string raw = "\"\"";
        for (int i = 0; i < 18; i++) raw = "{\"translate\":\"%1$s%1$s\",\"with\":[" + raw + "]}";
        Assert.Throws<ArgumentException>(() => new MotdParser().deserialize(raw));
    }

    [Theory]
    [InlineData("[\"§aA\",\"B\"]")]
    [InlineData("{\"text\":\"§aA\",\"extra\":[\"B\"]}")]
    public void InlineLegacyCodesDoNotChangeJavaJsonInheritance(string raw)
    {
        var contents = new MotdParser().deserialize(raw);
        Assert.Equal("#55FF55", contents[0].Color);
        Assert.Equal("#808080", contents[1].Color);
    }

    [Fact]
    public void JsonEscapingNeverDropsUnicodeQuotesBackslashesOrBlankLines()
    {
        var random = new Random(42);
        string[] tokens = { "한", "😀", "e\u0301", " ", "\t", "\r", "\n", "\\", "\"", "<", ">", "&", "{", "}", "A" };
        var parser = new MotdParser();
        for (int i = 0; i < 200; i++)
        {
            string text = string.Concat(Enumerable.Range(0, 50).Select(_ => tokens[random.Next(tokens.Length)]));
            string expected = text.Replace("\r\n", "\n").Replace('\r', '\n');
            Assert.Equal(expected, Plain(parser.deserialize(JsonSerializer.Serialize(text))));
            Assert.Equal(expected, Plain(parser.deserialize(JsonSerializer.Serialize(new { text }))));
        }
    }
}
