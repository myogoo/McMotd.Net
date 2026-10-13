using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using McMotdParser.Data;
using McMotdParser.Enum;

namespace McMotdParser.Deserializer;

internal class MotdDeserializer : JsonConverter<List<MotdContent>>
{
    private static readonly (string Name, TextFormatEnum Format)[] Formats =
    {
        ("bold", TextFormatEnum.Bold), ("italic", TextFormatEnum.Italic),
        ("underlined", TextFormatEnum.Underline), ("strikethrough", TextFormatEnum.Striktethrough),
        ("obfuscated", TextFormatEnum.Obfuscated)
    };
    private static readonly Regex TranslationSlot = new Regex(
        @"%%\d+|%%|%(?:(\d+)\$)?s", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public override List<MotdContent> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return Deserialize(document.RootElement, new MotdParserOptions());
    }

    public override void Write(Utf8JsonWriter writer, List<MotdContent> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var content in value)
        {
            writer.WriteStartObject();
            writer.WriteString("text", (content.LineBreak ? "\n" : "") + content.Text);
            writer.WriteString("color", MotdData.TryColor(content.Color, out var color) ? color : "#808080");
            foreach (var (name, format) in Formats) writer.WriteBoolean(name, content.TextFormatting.Contains(format));
            if (content.Font != null) writer.WriteString("font", content.Font);
            if (content.ShadowColor is uint shadow) writer.WriteNumber("shadow_color", shadow);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    internal static List<MotdContent> Deserialize(JsonElement element, MotdParserOptions options)
    {
        var builder = new MotdTextBuilder(options);
        ReadNode(element, new MotdContent(), builder, options, 0);
        return builder.Build();
    }

    private static MotdContent ReadNode(JsonElement element, MotdContent inherited, MotdTextBuilder builder,
        MotdParserOptions options, int depth)
    {
        if (depth > MotdParser.MaxDepth) throw new JsonException("MOTD component nesting is too deep.");
        builder.VisitComponent();
        if (element.ValueKind == JsonValueKind.Array)
        {
            var parent = inherited;
            bool first = true;
            foreach (var child in element.EnumerateArray())
            {
                var childStyle = ReadNode(child, parent, builder, options, depth + 1);
                if (first) parent = childStyle;
                first = false;
            }
            return parent;
        }
        if (element.ValueKind != JsonValueKind.Object)
        {
            var current = builder.Append(ValueText(element), inherited);
            return options.Edition == MinecraftEdition.Bedrock ? current : inherited;
        }

        var style = ReadStyle(element, inherited);
        if (element.TryGetProperty("rawtext", out var rawtext) && rawtext.ValueKind == JsonValueKind.Array)
        {
            var current = style;
            foreach (var child in rawtext.EnumerateArray())
                current = ReadNode(child, current, builder, options, depth + 1);
            return current;
        }
        // Accept a full Java status response as well as its description field.
        if (element.TryGetProperty("description", out var description) && !element.TryGetProperty("text", out _))
            return ReadNode(description, style, builder, options, depth + 1);

        var resultingStyle = style;
        if (element.TryGetProperty("text", out var text))
        {
            resultingStyle = builder.Append(ValueText(text), style);
        }
        else if (element.TryGetProperty("translate", out var translate))
        {
            resultingStyle = ReadTranslation(element, ValueText(translate), style, builder, options, depth + 1);
        }
        else if (element.TryGetProperty("keybind", out _) || element.TryGetProperty("selector", out _)
            || element.TryGetProperty("score", out _) || element.TryGetProperty("nbt", out _)
            || element.TryGetProperty("object", out _) || element.TryGetProperty("sprite", out _)
            || element.TryGetProperty("player", out _))
        {
            var componentStyle = style.CopyStyle();
            componentStyle.Component = element.Clone();
            string resolved = options.ComponentResolver?.Invoke(componentStyle.Component.Value) ?? ComponentFallback(element);
            resultingStyle = builder.Append(resolved, componentStyle);
        }

        if (element.TryGetProperty("extra", out var extra))
        {
            if (extra.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in extra.EnumerateArray())
                    ReadNode(child, style, builder, options, depth + 1);
            }
            else ReadNode(extra, style, builder, options, depth + 1);
        }
        return options.Edition == MinecraftEdition.Bedrock ? resultingStyle : style;
    }

    private static MotdContent ReadStyle(JsonElement element, MotdContent inherited)
    {
        var style = inherited.CopyStyle();
        style.Component = null;
        if (element.TryGetProperty("color", out var color))
        {
            var name = ValueText(color);
            if (name == "reset")
            {
                style.Color = "#808080";
                style.TextFormatting.Clear();
            }
            else if (MotdData.TryColor(name, out var parsedColor)) style.Color = parsedColor;
        }
        foreach (var (name, format) in Formats)
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            bool? enabled = value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when value.TryGetInt32(out int flag) => flag != 0,
                JsonValueKind.String when bool.TryParse(value.GetString(), out bool flag) => flag,
                _ => null
            };
            if (enabled == true) style.TextFormatting.Add(format);
            if (enabled == false) style.TextFormatting.Remove(format);
        }
        if (element.TryGetProperty("font", out var font) && font.ValueKind == JsonValueKind.String)
            style.Font = font.GetString();
        if (element.TryGetProperty("shadow_color", out var shadow))
        {
            if (shadow.ValueKind == JsonValueKind.Number && shadow.TryGetInt64(out long number)
                && number >= int.MinValue && number <= uint.MaxValue)
                style.ShadowColor = unchecked((uint)number);
            else if (shadow.ValueKind == JsonValueKind.Array && shadow.GetArrayLength() == 4)
            {
                var channels = shadow.EnumerateArray().ToArray();
                if (channels.All(channel => channel.ValueKind == JsonValueKind.Number
                    && channel.TryGetDouble(out var value) && value >= 0 && value <= 1))
                {
                    uint Channel(int index) => (uint)Math.Round(channels[index].GetDouble() * 255);
                    style.ShadowColor = (Channel(3) << 24) | (Channel(0) << 16) | (Channel(1) << 8) | Channel(2);
                }
            }
        }
        return style;
    }

    private static MotdContent ReadTranslation(JsonElement element, string key, MotdContent style, MotdTextBuilder builder,
        MotdParserOptions options, int depth)
    {
        string format = options.TranslationResolver?.Invoke(key) ?? PropertyText(element, "fallback") ?? key;
        var arguments = Array.Empty<JsonElement>();
        if (element.TryGetProperty("with", out var with))
        {
            if (with.ValueKind == JsonValueKind.Array) arguments = with.EnumerateArray().ToArray();
            else if (with.ValueKind == JsonValueKind.Object && with.TryGetProperty("rawtext", out var rawtext)
                && rawtext.ValueKind == JsonValueKind.Array) arguments = rawtext.EnumerateArray().ToArray();
        }
        if (format.Length > MotdParser.MaxInputLength) throw new ArgumentException("Translation is too long.");

        int position = 0;
        int nextArgument = 0;
        foreach (Match match in TranslationSlot.Matches(format))
        {
            style = builder.Append(format.Substring(position, match.Index - position), style);
            int argument = -1;
            if (match.Value == "%%") style = builder.Append("%", style);
            else if (match.Value.StartsWith("%%", StringComparison.Ordinal))
            {
                if (options.Edition == MinecraftEdition.Bedrock
                    && int.TryParse(match.Value.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    argument = index > 0 ? index - 1 : -1;
                else style = builder.Append(match.Value.Substring(1), style);
            }
            else if (match.Groups[1].Success)
            {
                if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    argument = index > 0 ? index - 1 : -1;
            }
            else argument = nextArgument++;

            if (argument >= 0 && argument < arguments.Length)
            {
                var argumentStyle = ReadNode(arguments[argument], style, builder, options, depth + 1);
                if (options.Edition == MinecraftEdition.Bedrock)
                {
                    style = argumentStyle;
                    style.Component = null;
                }
            }
            else if (match.Value != "%%" && !match.Value.StartsWith("%%", StringComparison.Ordinal)
                && options.Edition == MinecraftEdition.Java)
                style = builder.Append(match.Value, style);
            position = match.Index + match.Length;
        }
        return builder.Append(format.Substring(position), style);
    }

    private static string ComponentFallback(JsonElement element)
    {
        if (PropertyText(element, "fallback") is string fallback) return fallback;
        if (PropertyText(element, "keybind") is string keybind) return keybind;
        if (PropertyText(element, "selector") is string selector) return selector;
        if (element.TryGetProperty("score", out var score) && score.ValueKind == JsonValueKind.Object)
            return PropertyText(score, "value") ?? $"[score:{PropertyText(score, "name")}/{PropertyText(score, "objective")}]";
        if (PropertyText(element, "nbt") is string nbt) return $"[nbt:{nbt}]";
        if (PropertyText(element, "sprite") is string sprite) return $"[sprite:{sprite}]";
        if (element.TryGetProperty("player", out var player))
            return $"[player:{(player.ValueKind == JsonValueKind.Object ? PropertyText(player, "name") ?? PropertyText(player, "id") : ValueText(player))}]";
        return "[object]";
    }

    private static string? PropertyText(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? ValueText(value) : null;

    private static string ValueText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
        _ => string.Empty
    };
}
