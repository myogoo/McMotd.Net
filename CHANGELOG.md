# Changelog

## 0.1.0

- Parse Java and Bedrock MOTD strings, RGB colors, nested JSON components, inherited styles, translations, and line breaks.
- Add explicit edition, configuration-escaping, translation, and contextual-component options.
- Encode HTML output and bound input size, nesting, and translation expansion.
- Preserve font, shadow, and contextual-component metadata with documented text fallbacks.
- Update Razor and MAUI when input changes; animate obfuscated text and stop background work on disposal/unload.
- Fix quote/backslash parsing, unknown legacy codes, lost formatting, empty-node spacing, and equality/hash consistency.
- Add regression and renderer lifecycle coverage (128 passing tests).

Existing public method names remain available. Corrected whitespace and HTML output can differ from previous versions. Artificial section-sign newline markers are removed. Resource-pack assets and world-dependent components require caller-provided rendering/context. See [COMPATIBILITY.md](COMPATIBILITY.md) for the full contract and limits.
