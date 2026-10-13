# McMotd.Net.Razor

```razor
@using McMotdParser
@using McMotdParser.Razor

<McMotdComponent RawMotdString="@motd" ParserOptions="@options" />

@code {
    private string motd = "§aHello §lMinecraft!";
    private MotdParserOptions options = new() { Edition = MinecraftEdition.Java };
}
```

RawMotdString and ParserOptions are re-applied on parameter updates. Rendering uses the core HTML encoder, retains whitespace and combined decorations, and animates obfuscated text while the component is alive. Disposal cancels the timer.

Options are optional; Java is the default. Resource-pack assets need a custom renderer; parsed metadata/text fallbacks are not a pixel-identical Minecraft client.
