using System.Net;
using System.Text;
using System.Text.Json;
using System.Globalization;
using McMotdParser.Data;
using McMotdParser.Deserializer;
using McMotdParser.Enum;
using McMotdParser.Utils;

namespace McMotdParser;

public class MotdParser
{
    internal const int MaxInputLength = 262144;
    internal const int MaxDepth = 128;
    private readonly MotdParserOptions options;

    public MotdParser() : this(new MotdParserOptions()) { }

    public MotdParser(MotdParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!System.Enum.IsDefined(options.Edition)) throw new ArgumentOutOfRangeException(nameof(options.Edition));
        if (char.IsControl(options.LegacyCodeCharacter) || char.IsSurrogate(options.LegacyCodeCharacter))
            throw new ArgumentException("The legacy code character must be a printable character.", nameof(options));
        this.options = options;
    }

    public List<MotdContent> deserialize(string RawMotd)
    {
        ArgumentNullException.ThrowIfNull(RawMotd);
        if (RawMotd.Length > MaxInputLength) throw new ArgumentException("MOTD is too long.", nameof(RawMotd));
        var trimmed = RawMotd.AsSpan().TrimStart();
        if (!trimmed.IsEmpty && trimmed[0] is '{' or '[' or '"')
        {
            JsonDocument? document = null;
            try
            {
                document = JsonDocument.Parse(EscapeJsonControls(RawMotd), new JsonDocumentOptions { MaxDepth = MaxDepth });
            }
            catch (JsonException)
            {
                // A MOTD may be ordinary text beginning with a brace or quotation mark.
            }
            if (document != null)
            {
                using (document) return deserialize(document.RootElement);
            }
        }

        if (options.InterpretEscapeSequences) RawMotd = StringUtils.DecodeEscapes(RawMotd);
        return new SectionSignDeserializer(RawMotd, options).deserialize();
    }

    public List<MotdContent> deserialize(JsonElement component)
    {
        if (component.GetRawText().Length > MaxInputLength) throw new ArgumentException("MOTD is too long.", nameof(component));
        return MotdDeserializer.Deserialize(component, options);
    }

    public string ToHtml(string RawMotd) => RenderHtml(deserialize(RawMotd));

    public string RenderHtml(IEnumerable<MotdContent> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        var html = new StringBuilder("<div class=\"mcmotd-container\" style=\"white-space:pre-wrap;overflow-wrap:anywhere;\">");
        foreach (var content in contents)
        {
            if (content.LineBreak) html.Append("<br/>");
            var color = MotdData.TryColor(content.Color, out var parsedColor) ? parsedColor : "#808080";
            html.Append("<span style=\"color:").Append(color).Append(';');
            var formats = content.TextFormatting;
            if (formats.Contains(TextFormatEnum.Bold)) html.Append("font-weight:bold;");
            if (formats.Contains(TextFormatEnum.Italic)) html.Append("font-style:italic;");
            bool underline = formats.Contains(TextFormatEnum.Underline);
            bool strike = formats.Contains(TextFormatEnum.Striktethrough);
            if (underline || strike)
            {
                html.Append("text-decoration:").Append(underline ? "underline" : "line-through");
                if (underline && strike) html.Append(" line-through");
                html.Append(';');
            }
            if (content.ShadowColor is uint shadow)
            {
                html.Append("text-shadow:1px 1px rgba(")
                    .Append((shadow >> 16) & 255).Append(',').Append((shadow >> 8) & 255).Append(',')
                    .Append(shadow & 255).Append(',')
                    .Append(((shadow >> 24) / 255.0).ToString("0.###", CultureInfo.InvariantCulture)).Append(");");
            }
            html.Append('"');
            if (content.Font != null)
                html.Append(" data-mcmotd-font=\"").Append(WebUtility.HtmlEncode(content.Font)).Append('"');
            if (formats.Contains(TextFormatEnum.Obfuscated))
                html.Append(" role=\"img\" aria-label=\"").Append(WebUtility.HtmlEncode(content.Text)).Append("\" class=\"mcmotd-obfuscated\"");
            html.Append('>').Append(WebUtility.HtmlEncode(content.GetDisplayText())).Append("</span>");
        }
        return html.Append("</div>").ToString();
    }

    internal static string EscapeJsonControls(string text)
    {
        var result = new StringBuilder(text.Length);
        bool inString = false;
        bool escaped = false;
        foreach (char character in text)
        {
            if (inString && character < ' ')
            {
                result.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
            }
            else result.Append(character);
            if (character == '"' && !escaped) inString = !inString;
            escaped = character == '\\' && !escaped;
        }
        return result.ToString();
    }
}
