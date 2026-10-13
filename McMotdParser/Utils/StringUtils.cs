using System.Text;
using System.Text.Json;

namespace McMotdParser.Utils;

public class StringUtils
{
    public static string ToJsonObjectString(string motd) => "{ \"Contents\" : " + motd + " }";

    public static string EscapeCharacterReplace(string motd)
    {
        ArgumentNullException.ThrowIfNull(motd);
        var trimmed = motd.AsSpan().TrimStart();
        if (!trimmed.IsEmpty && trimmed[0] is '{' or '[' or '"')
        {
            var normalized = MotdParser.EscapeJsonControls(motd);
            try
            {
                using var document = JsonDocument.Parse(normalized, new JsonDocumentOptions { MaxDepth = MotdParser.MaxDepth });
                return normalized;
            }
            catch (JsonException) { }
        }
        return JsonSerializer.Serialize(motd)[1..^1];
    }

    public static string QuotesREMOVE(string motd)
    {
        ArgumentNullException.ThrowIfNull(motd);
        if (motd.StartsWith('"'))
        {
            try { return JsonSerializer.Deserialize<string>(motd) ?? string.Empty; }
            catch (JsonException) { }
        }
        return motd;
    }

    public static string QuotesPlace(string motd)
    {
        ArgumentNullException.ThrowIfNull(motd);
        var trimmed = motd.AsSpan().TrimStart();
        if (!trimmed.IsEmpty && trimmed[0] is '{' or '[' or '"')
        {
            var normalized = MotdParser.EscapeJsonControls(motd);
            try
            {
                using var document = JsonDocument.Parse(normalized, new JsonDocumentOptions { MaxDepth = MotdParser.MaxDepth });
                return normalized;
            }
            catch (JsonException) { }
        }
        return JsonSerializer.Serialize(motd);
    }

    public static string DecodeEscapes(string motd)
    {
        ArgumentNullException.ThrowIfNull(motd);
        var result = new StringBuilder(motd.Length);
        for (int i = 0; i < motd.Length; i++)
        {
            if (motd[i] != '\\' || i + 1 == motd.Length)
            {
                result.Append(motd[i]);
                continue;
            }
            char escaped = motd[i + 1];
            if (escaped == 'u' && i + 6 <= motd.Length
                && ushort.TryParse(motd.AsSpan(i + 2, 4), System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var unicode))
            {
                result.Append((char)unicode);
                i += 5;
            }
            else if (escaped is 'n' or 'r' or 't' or 'f' or '\\' or '"' or '\'')
            {
                result.Append(escaped switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'f' => '\f', _ => escaped });
                i++;
            }
            else result.Append(motd[i]);
        }
        return result.ToString();
    }
}
