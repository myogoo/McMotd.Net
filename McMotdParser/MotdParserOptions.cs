using System.Text.Json;

namespace McMotdParser;

public enum MinecraftEdition
{
    Java,
    Bedrock
}

public sealed class MotdParserOptions
{
    public MinecraftEdition Edition { get; init; } = MinecraftEdition.Java;
    public char LegacyCodeCharacter { get; init; } = '§';
    // Enable only for escaped configuration values, not decoded server responses.
    public bool InterpretEscapeSequences { get; init; }
    public Func<string, string?>? TranslationResolver { get; init; }
    // Supplies text for keybinds, selectors, scores, NBT and resource-pack objects.
    public Func<JsonElement, string?>? ComponentResolver { get; init; }
}
