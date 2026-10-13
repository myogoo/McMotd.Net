using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using McMotdParser.Razor;

namespace McMotdParser.Test;

#pragma warning disable BL0006 // Exercise the component through Blazor's actual render lifecycle.
public class RazorComponentTests
{
    [Fact]
    public async Task ParameterUpdatesReplaceOldTextAndKeepUntrustedTextEncoded()
    {
        await using var renderer = new TestRenderer();
        int id = renderer.Add(new McMotdComponent());
        await renderer.Render(id, "first");
        Assert.Contains("first", await renderer.Markup(id));

        await renderer.Render(id, "<script>second</script>");
        var html = await renderer.Markup(id);
        Assert.DoesNotContain("first", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;second&lt;/script&gt;", html);
    }

    [Fact]
    public async Task BedrockOptionsReachTheSharedRenderer()
    {
        await using var renderer = new TestRenderer();
        int id = renderer.Add(new McMotdComponent());
        await renderer.Render(id, "§mText", new MotdParserOptions { Edition = MinecraftEdition.Bedrock });
        var html = await renderer.Markup(id);
        Assert.Contains("color:#971607;", html);
        Assert.DoesNotContain("line-through", html);
    }

    [Fact]
    public async Task ObfuscatedTextAnimatesAndStopsAfterParameterUpdate()
    {
        await using var renderer = new TestRenderer();
        int id = renderer.Add(new McMotdComponent());
        await renderer.Render(id, "§kHello");
        await renderer.SecondRender.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("aria-label=\"Hello\"", await renderer.Markup(id));

        await renderer.Render(id, "Plain");
        int renderCount = renderer.RenderCount;
        await Task.Delay(250);
        Assert.Equal(renderCount, renderer.RenderCount);
        Assert.DoesNotContain("mcmotd-obfuscated", await renderer.Markup(id));
    }

    [Fact]
    public async Task RapidUpdatesAndDisposalCancelObfuscationWithoutLeakingRenders()
    {
        var renderer = new TestRenderer();
        int id = renderer.Add(new McMotdComponent());
        await renderer.Render(id, "§kFirst");
        await Task.WhenAll(renderer.Render(id, "§kSecond"), renderer.Render(id, "§kThird"));
        await renderer.Render(id, "Final");
        Assert.Contains("Final", await renderer.Markup(id));
        await renderer.Render(id, "§kLast");
        await renderer.DisposeAsync();
        int renderCount = renderer.RenderCount;
        await Task.Delay(250);
        Assert.Equal(renderCount, renderer.RenderCount);
    }

    private sealed class TestRenderer : Renderer
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public int RenderCount { get; private set; }
        public TaskCompletionSource SecondRender { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestRenderer() : base(new EmptyServices(), NullLoggerFactory.Instance) { }

        public int Add(IComponent component) => AssignRootComponentId(component);

        public Task Render(int id, string raw, MotdParserOptions? options = null) => Dispatcher.InvokeAsync(() =>
            RenderRootComponentAsync(id, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(McMotdComponent.RawMotdString)] = raw,
                [nameof(McMotdComponent.ParserOptions)] = options
            })));

        public Task<string> Markup(int id) => Dispatcher.InvokeAsync(() =>
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return string.Concat(frames.Array.Take(frames.Count)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Markup).Select(frame => frame.MarkupContent));
        });

        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Component failed", exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            RenderCount++;
            if (RenderCount >= 2) SecondRender.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
#pragma warning restore BL0006
