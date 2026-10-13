# McMotd.Net

Minecraft Java and Bedrock MOTD parsing and safe HTML rendering.

```csharp
using McMotdParser;

var parser = new MotdParser();
var html = parser.ToHtml("§aHello §lMinecraft!\n§rSecond line");
var runs = parser.deserialize(rawMotd);

var bedrock = new MotdParser(new MotdParserOptions
{
    Edition = MinecraftEdition.Bedrock
});
```

The parser accepts plain text, JSON strings/objects/arrays, nested Java components, status descriptions, and Bedrock rawtext. It preserves Unicode and line breaks, supports standard/hex colors and inherited styles, and encodes HTML output.

For escaped configuration values, set InterpretEscapeSequences; for & codes set LegacyCodeCharacter. Both are opt-in to avoid changing literal input.

TranslationResolver supplies locale templates. ComponentResolver supplies text for client/server/resource-dependent components. Without those resources, the parser retains source metadata and uses readable fallbacks; it cannot recreate custom fonts, textures, player heads or world state from text alone.

ToHtml produces a static obfuscation snapshot. The Razor and MAUI components can animate obfuscated text. HTML output and empty-node/line-break behavior differ from the original 0.0.3 implementation.

See the repository's COMPATIBILITY.md for details and limits.
