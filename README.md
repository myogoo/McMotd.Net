# McMotd.Net

Parse Minecraft Java and Bedrock MOTD text into styled runs, HTML, Razor, or MAUI.

```csharp
using McMotdParser;

var parser = new MotdParser();
string html = parser.ToHtml("§aHello §lMinecraft!\n§rSecond line");
List<MotdContent> runs = parser.deserialize(rawMotd);
```

Java is the default. Select Bedrock explicitly because codes such as §m and §n have different meanings:

```csharp
var bedrock = new MotdParser(new MotdParserOptions
{
    Edition = MinecraftEdition.Bedrock
});
```

Supported inputs include plain text, JSON strings/objects/arrays, nested Java components, Java status descriptions, and Bedrock rawtext. Formatting includes legacy colors, RGB codes, inherited JSON styles, explicit false overrides, combined decorations, and all line breaks.

See [COMPATIBILITY.md](COMPATIBILITY.md) for the complete input contract, translation and component resolvers, security limits, intentional behavior changes, and rendering limitations. Resource-pack assets and server/client context cannot be reconstructed from a MOTD string alone.

## Tests

With a .NET SDK and .NET 7 runtime:

```sh
dotnet test McMotdParser.Test/McMotdParser.Test.csproj --configuration Release
dotnet build McMotdParser.Razor/McMotdParser.Razor.csproj --configuration Release
```

Tests cover the parser, HTML output, and Razor's actual parameter-update and animation lifecycle. They do not require MAUI workloads. The full solution includes MAUI platform targets and requires their workloads.
