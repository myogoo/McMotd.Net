using McMotdParser.Deserializer;
using McMotdParser.Enum;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;
using System.Text;

namespace McMotdParser;
public class MotdContent
{
    public string Color { get; set; } = "#808080";
    public string Text { get; set; } = string.Empty;
    public HashSet<TextFormatEnum> TextFormatting { get; set; } = new HashSet<TextFormatEnum>();
    public bool LineBreak { get; set; } = false;
    public string? Font { get; set; }
    public uint? ShadowColor { get; set; }
    public JsonElement? Component { get; set; }

    internal MotdContent CopyStyle() => new MotdContent
    {
        Color = Color,
        TextFormatting = new HashSet<TextFormatEnum>(TextFormatting),
        Font = Font,
        ShadowColor = ShadowColor,
        Component = Component
    };

    public string GetDisplayText()
    {
        if (!TextFormatting.Contains(TextFormatEnum.Obfuscated)) return Text;
        var result = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(Text);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            result.Append(string.IsNullOrWhiteSpace(element) ? element : ((char)Random.Shared.Next(33, 127)).ToString());
        }
        return result.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is MotdContent target
            && obj.GetType() == typeof(MotdContent)
            && Text == target.Text
            && Color == target.Color
            && LineBreak == target.LineBreak
            && Font == target.Font
            && ShadowColor == target.ShadowColor
            && Component?.GetRawText() == target.Component?.GetRawText()
            && TextFormatting.SetEquals(target.TextFormatting);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text, Color, LineBreak, Font, ShadowColor, Component?.GetRawText(),
            HashSet<TextFormatEnum>.CreateSetComparer().GetHashCode(TextFormatting));
    }
}


public class MotdContents
{
    [JsonConverter(typeof(MotdDeserializer))]
    public List<MotdContent> Contents { get; set; } = new List<MotdContent>();

    public override bool Equals(object? obj)
    {
        return obj is MotdContents target
            && obj.GetType() == typeof(MotdContents)
            && Contents.SequenceEqual(target.Contents);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var content in Contents)
        {
            hash.Add(content);
        }
        return hash.ToHashCode();
    }
}
