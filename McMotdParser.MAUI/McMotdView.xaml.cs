using McMotdParser.Enum;

namespace McMotdParser.MAUI;

public partial class McMotdView : ContentView
{
    public static readonly BindableProperty RawMotdStringProperty = BindableProperty.Create(
        nameof(RawMotdString), typeof(string), typeof(McMotdView), string.Empty,
        propertyChanged: (view, _, _) => ((McMotdView)view).Render());

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double?), typeof(McMotdView), null,
        propertyChanged: (view, _, _) => ((McMotdView)view).Render());

    public static readonly BindableProperty ParserOptionsProperty = BindableProperty.Create(
        nameof(ParserOptions), typeof(MotdParserOptions), typeof(McMotdView), null,
        propertyChanged: (view, _, _) => ((McMotdView)view).Render());

    public string RawMotdString
    {
        get => (string)GetValue(RawMotdStringProperty);
        set => SetValue(RawMotdStringProperty, value);
    }

    public double? FontSize
    {
        get => (double?)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public MotdParserOptions ParserOptions
    {
        get => (MotdParserOptions)GetValue(ParserOptionsProperty);
        set => SetValue(ParserOptionsProperty, value);
    }

    private IDispatcherTimer animation;
    private readonly List<(MotdContent Content, Span Span)> obfuscated = new();

    public McMotdView()
    {
        InitializeComponent();
        Unloaded += (_, _) => animation?.Stop();
    }

    private void McMotdView_OnLoaded(object sender, EventArgs e) => Render();

    private void Render()
    {
        if (MotdStackLayout == null) return;
        animation?.Stop();
        obfuscated.Clear();
        var contents = new MotdParser(ParserOptions ?? new MotdParserOptions()).deserialize(RawMotdString ?? string.Empty);
        var formatted = new FormattedString();
        foreach (var content in contents)
        {
            var span = new Span
            {
                Text = (content.LineBreak ? "\n" : "") + content.GetDisplayText(),
                TextColor = Color.FromArgb(content.Color)
            };
            var formats = content.TextFormatting;
            if (formats.Contains(TextFormatEnum.Bold)) span.FontAttributes |= FontAttributes.Bold;
            if (formats.Contains(TextFormatEnum.Italic)) span.FontAttributes |= FontAttributes.Italic;
            if (formats.Contains(TextFormatEnum.Underline)) span.TextDecorations |= TextDecorations.Underline;
            if (formats.Contains(TextFormatEnum.Striktethrough)) span.TextDecorations |= TextDecorations.Strikethrough;
            formatted.Spans.Add(span);
            if (formats.Contains(TextFormatEnum.Obfuscated)) obfuscated.Add((content, span));
        }
        var label = new Label { FontSize = FontSize ?? 12, FormattedText = formatted };
        SemanticProperties.SetDescription(label,
            string.Concat(contents.Select(content => (content.LineBreak ? "\n" : "") + content.Text)));
        MotdStackLayout.Children.Clear();
        MotdStackLayout.Children.Add(label);
        if (IsLoaded && obfuscated.Count > 0)
        {
            if (animation == null)
            {
                animation = Dispatcher.CreateTimer();
                animation.Interval = TimeSpan.FromMilliseconds(100);
                animation.Tick += (_, _) =>
                {
                    foreach (var (content, span) in obfuscated)
                        span.Text = (content.LineBreak ? "\n" : "") + content.GetDisplayText();
                };
            }
            animation.Start();
        }
    }
}
