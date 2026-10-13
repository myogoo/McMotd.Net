namespace McMotdParser.Utils;

public class StringUtilsBuilder
{
    private string? RawMotd;

    public string build()
    {
        if (RawMotd is null) throw new ArgumentNullException("RawMotd is not setted");
        return RawMotd;
    }

    public StringUtilsBuilder setString(string RawMotd)
    {
        ArgumentNullException.ThrowIfNull(RawMotd);
        this.RawMotd = RawMotd;
        return this;
    }
    public StringUtilsBuilder QuotePlace()
    {
        this.RawMotd = StringUtils.QuotesPlace(build());
        return this;
    }

    public StringUtilsBuilder ToJsonObjectString()
    {
        this.RawMotd = StringUtils.ToJsonObjectString(build());
        return this;
    }

    public StringUtilsBuilder EscapeCharacterReplace()
    {
        this.RawMotd = StringUtils.EscapeCharacterReplace(build());
        return this;
    }
    
}
