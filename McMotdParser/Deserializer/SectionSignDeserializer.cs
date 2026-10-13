namespace McMotdParser.Deserializer;

public class SectionSignDeserializer
{
    private readonly string rawMotd;
    private readonly MotdParserOptions options;

    public SectionSignDeserializer(string RawMotd) : this(RawMotd, new MotdParserOptions()) { }

    public SectionSignDeserializer(string RawMotd, MotdParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(RawMotd);
        ArgumentNullException.ThrowIfNull(options);
        if (RawMotd.Length > MotdParser.MaxInputLength) throw new ArgumentException("MOTD is too long.", nameof(RawMotd));
        rawMotd = RawMotd;
        this.options = options;
    }

    public List<MotdContent> deserialize()
    {
        var builder = new MotdTextBuilder(options);
        builder.Append(rawMotd, new MotdContent());
        return builder.Build();
    }
}
