# McMotd.Net.MAUI

```xml
xmlns:motd="clr-namespace:McMotdParser.MAUI;assembly=McMotdParser.MAUI"

<motd:McMotdView RawMotdString="{Binding ServerMotd}" FontSize="24" />
```

RawMotdString, FontSize and ParserOptions are bindable properties. Set ParserOptions to a MotdParserOptions instance to select Bedrock or provide translation/component text resolvers.

A formatted label preserves line breaks and combines bold, italic, underline and strikethrough. Reloading replaces existing content; obfuscation animation stops when unloaded.

The standard renderer uses text fallbacks for resource-pack objects/custom fonts and does not implement per-span shadow_color. Use parsed runs with a custom renderer when those resources are available.
