# MOTD compatibility

## Input contract

The library parses the **display content** of server MOTDs. It is not a Java TCP/RakNet packet decoder, a server query client, an SNBT command parser, or a MiniMessage configuration parser. Such formats must first be decoded by their owning protocol or plugin.

| Input | Behavior |
| --- | --- |
| Plain decoded string | Preserves Unicode, quotes, backslashes, whitespace, and unknown/truncated codes |
| JSON string | Decodes JSON escaping once, then reads inline legacy formatting |
| Java JSON object | Recursively reads text and extra, including inline legacy formatting |
| Java JSON array | First component supplies the inherited JSON style for later members |
| Java full status JSON | Reads description |
| Bedrock rawtext | Reads nodes in order; Bedrock mode carries inline formatting between nodes |
| Empty, numeric, boolean or null JSON content | Text primitives render as text; null/empty nodes add no invented spaces |
| Malformed JSON-looking string | Preserved as literal text and safely HTML-encoded |
| Already parsed JSON | Use deserialize(JsonElement); invalid/deep components report a bounded parsing error |

The default text color remains #808080 for compatibility with this library. CRLF and CR normalize to LF. Leading, repeated, internal and trailing line breaks are retained through MotdContent.LineBreak; a final empty run represents a trailing newline.

For plain text that deliberately looks exactly like a JSON string/object, use SectionSignDeserializer directly to bypass JSON detection.

## Java versus Bedrock

Set MotdParserOptions.Edition. Automatic detection from a legacy string is impossible: §m and §n mean decorations in Java and material colors in modern Bedrock.

- Both editions: standard 0–f colors, bold, italic, obfuscated text, reset.
- Java: strikethrough and underline; applying a color resets active decorations.
- Bedrock: applying a color keeps active decorations. Extra colors g/h/i/j/m/n/p/q/s/t/u/v/w include materials, resin, and party-chat light blue.
- RGB: §#RRGGBB and BungeeCord's §x§R§R§G§G§B§B.
- Uppercase legacy code letters are accepted.
- Reset restores the library's default color and clears decorations.
- Unknown and incomplete codes are preserved instead of crashing. No supported-version claim is made for invented future codes.

The Bedrock profile uses current color meanings, not a historical version emulator.

## Configuration escaping and ampersands

Decoded network responses do not need another escaping pass. Literal backslashes and ampersands remain literal by default.

For an escaped configuration value, opt in explicitly:

```csharp
var parser = new MotdParser(new MotdParserOptions
{
    InterpretEscapeSequences = true,
    LegacyCodeCharacter = '&'
});
var runs = parser.deserialize(@"\u00a7aHello\n&lWorld");
```

Escapes are decoded once on the non-JSON path: Unicode escape units, newline, carriage return, tab, form feed, backslash and quoted characters. Unknown/incomplete escape sequences remain literal. This is a value decoder, not a parser for entire server.properties files.

Ampersand mode also accepts §. MiniMessage tags and plugin-specific placeholder languages need their own parser before this library.

## JSON components and context

Nested extra and arrays inherit colors and decorations; explicit false removes an inherited decoration. Child overrides do not leak into siblings. Inline Java § codes affect their own text run, not the JSON style inherited by siblings. Empty containers do not create blank text.

| Component | Rendering |
| --- | --- |
| text | Styled text, including real line breaks and inline legacy codes |
| translate + with | TranslationResolver, then fallback, then the key itself; styled component arguments remain styled |
| keybind | ComponentResolver, or binding identifier |
| selector | ComponentResolver, or selector expression |
| score | ComponentResolver, or supplied value, or an explicit score placeholder |
| nbt | ComponentResolver, or an explicit NBT-path placeholder |
| object / sprite / player | ComponentResolver, or fallback / descriptive asset placeholder |

Java translation slots support %s, %n$s and %%. Bedrock also supports %%1-style slots and with.rawtext arguments. Missing Bedrock arguments render empty; missing Java arguments retain their placeholder instead of throwing.

Supply the locale data used by the target client:

```csharp
var translations = new Dictionary<string, string>
{
    ["server.welcome"] = "Welcome, %s!"
};
var parser = new MotdParser(new MotdParserOptions
{
    TranslationResolver = key => translations.GetValueOrDefault(key),
    ComponentResolver = component =>
        component.TryGetProperty("keybind", out var key) && key.GetString() == "key.jump"
            ? "Space"
            : null
});
```

ComponentResolver resolves the component's own display text; extra children are appended separately. Callback output is still treated as text and HTML-encoded. Callbacks should be thread-safe if a parser is shared.

Context-dependent runs retain an owned JsonElement in MotdContent.Component for custom rendering/resolution. Font identifiers and shadow colors are inherited and retained too. The library does not download skins, fonts, textures, language files, or server world state. NBT selectors/scores cannot be evaluated without that state. Unknown translation keys cannot be localized without language data.

Click/hover/insertion events are not activated: a server-list MOTD is a display surface, not a command execution surface.

## Renderers

| Feature | HTML string | Razor | MAUI |
| --- | --- | --- | --- |
| Colors, RGB, Unicode and line breaks | Yes | Yes | Yes |
| Bold + italic + underline + strike combinations | Yes | Yes | Yes |
| Obfuscated text | Randomized snapshot per render | Updates every 100 ms | Updates every 100 ms while loaded |
| Original obfuscated text | Retained in runs and accessible label | Same | Semantic description |
| Input changes | Call ToHtml again | Parameter updates rebuild content | Bindable property updates rebuild content |
| Re-loading/disposal | No background work | Cancels animation | Stops animation when unloaded; replaces old children |
| shadow_color | CSS shadow | CSS shadow | Metadata retained; no per-span shadow |
| Custom font / sprite / player assets | Metadata or text fallback | Metadata or text fallback | Metadata or text fallback |

Obfuscation operates on Unicode text elements and keeps whitespace; it does not claim Minecraft's exact font-width matching. HTML output includes no executable scripts or event handlers. Standalone HTML animation requires a host to re-render snapshots; Razor and MAUI provide animation.

Font identifiers are emitted only as encoded data attributes in HTML, never as arbitrary CSS. A custom asset renderer can consume the parsed runs. Resource-pack glyphs and objects are **not automatically rendered as the original image**.

## Safety and intentional compatibility changes

- Text and HTML attributes are encoded. Colors must be named standard colors or exactly #RRGGBB; invalid colors inherit the parent/default.
- Input and expanded text are limited to 262,144 UTF-16 units, JSON nesting to 128, and expansion to 32,768 component visits. Oversized input/expansion throws an argument/parsing exception. These are denial-of-service limits, not claims of supporting arbitrarily large input.
- No new third-party package dependency was added. Existing xUnit and Razor dependencies provide the tests and UI.
- Public names such as deserialize, SectionSignDeserializer, StringUtilsBuilder and the existing enum values remain available.
- The old artificial §x/§z newline markers are retired. Use actual newlines, valid JSON escaping, or explicit InterpretEscapeSequences. StringUtils.EscapeCharacterReplace now produces JSON escapes.
- Empty JSON nodes no longer turn into spaces. Color state, reset, inherited styles, and internal/trailing newlines now follow the parser contract above.
- HTML whitespace/markup changed to encoded text with pre-wrap and combined decoration styles. Consumers must not depend on the old exact HTML string.
- MotdContents JSON serialization emits normalized, flattened styled text. It is not a lossless reconstruction of the original component tree or resource metadata.

## Sources checked

- [Microsoft Bedrock raw-message format](https://learn.microsoft.com/en-us/minecraft/creator/reference/content/rawmessagejson?view=minecraft-bedrock-stable)
- [Bedrock 1.26.30 update: party-chat color code](https://learn.microsoft.com/en-us/minecraft/creator/documents/update1.26.30?view=minecraft-bedrock-stable)
- [Adventure legacy/RGB formats](https://docs.papermc.io/adventure/serializer/legacy/)
- [Adventure JSON serializer compatibility](https://docs.papermc.io/adventure/serializer/gson/)
- [Minecraft Java 1.21.9 object components](https://www.minecraft.net/en-us/article/minecraft-java-edition-1-21-9)
- [PocketMine formatting constants](https://apidoc.pmmp.io/da/d59/_text_format_8php_source.html)
- [CustomToast's Bedrock color mapping](https://github.com/NhanAZ-Libraries/CustomToast)

Compatibility is established by documented cases and automated tests. No finite test suite guarantees every server, resource pack, mod, protocol version or future Minecraft release.
