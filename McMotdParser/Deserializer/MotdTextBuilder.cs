using System.Text;
using McMotdParser.Data;

namespace McMotdParser.Deserializer;

internal sealed class MotdTextBuilder
{
    private readonly MotdParserOptions options;
    private readonly List<MotdContent> contents = new List<MotdContent>();
    private bool lineBreak;
    private bool afterCarriageReturn;
    private MotdContent lastStyle = new MotdContent();
    private int outputLength;
    private int componentCount;

    internal MotdTextBuilder(MotdParserOptions options) => this.options = options;

    internal void VisitComponent()
    {
        if (++componentCount > 32768) throw new ArgumentException("MOTD expands into too many components.");
    }

    internal MotdContent Append(string text, MotdContent inherited)
    {
        outputLength += text.Length;
        if (outputLength > MotdParser.MaxInputLength)
            throw new ArgumentException("Expanded MOTD exceeds the supported length.");
        var style = inherited.CopyStyle();
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length == 0) return;
            var content = style.CopyStyle();
            content.Text = buffer.ToString();
            content.LineBreak = lineBreak;
            contents.Add(content);
            buffer.Clear();
            lineBreak = false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '\n' && afterCarriageReturn)
            {
                afterCarriageReturn = false;
                continue;
            }
            afterCarriageReturn = character == '\r';
            if (character == '\r' || character == '\n')
            {
                Flush();
                if (lineBreak) AddEmptyLine(style);
                lineBreak = true;
                continue;
            }

            if ((character == '§' || character == options.LegacyCodeCharacter) && i + 1 < text.Length)
            {
                char code = char.ToLowerInvariant(text[i + 1]);
                string? color = null;
                int codeLength = 2;
                if (code == '#' && i + 8 <= text.Length && MotdData.TryColor(text.Substring(i + 1, 7), out var rgb))
                {
                    color = rgb;
                    codeLength = 8;
                }
                else if (code == 'x' && i + 14 <= text.Length)
                {
                    var hex = new StringBuilder("#");
                    for (int digit = 0; digit < 6; digit++)
                    {
                        int position = i + 2 + digit * 2;
                        if (text[position] != character || !Uri.IsHexDigit(text[position + 1])) break;
                        hex.Append(text[position + 1]);
                    }
                    if (hex.Length == 7)
                    {
                        color = hex.ToString();
                        codeLength = 14;
                    }
                }
                else if (options.Edition == MinecraftEdition.Bedrock && MotdData.BedrockColors.TryGetValue(code, out var material))
                {
                    color = material;
                }
                else if (MotdData.ColorDict.TryGetValue(code.ToString(), out var legacy))
                {
                    color = legacy;
                }

                if (color != null)
                {
                    Flush();
                    style.Color = color;
                    if (options.Edition == MinecraftEdition.Java) style.TextFormatting.Clear();
                    i += codeLength - 1;
                    continue;
                }
                if (code == 'r')
                {
                    Flush();
                    style.Color = "#808080";
                    style.TextFormatting.Clear();
                    i++;
                    continue;
                }
                if (code is 'k' or 'l' or 'm' or 'n' or 'o')
                {
                    Flush();
                    style.TextFormatting.Add(MotdData.TextFormatDict[code.ToString()]);
                    i++;
                    continue;
                }
            }
            buffer.Append(character);
        }
        Flush();
        lastStyle = style;
        return style;
    }

    private void AddEmptyLine(MotdContent style)
    {
        var content = style.CopyStyle();
        content.LineBreak = true;
        contents.Add(content);
    }

    internal List<MotdContent> Build()
    {
        if (lineBreak) AddEmptyLine(lastStyle);
        if (contents.Count == 0) contents.Add(lastStyle.CopyStyle());
        return contents;
    }
}
