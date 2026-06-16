using Microsoft.Extensions.Options;

namespace OptionsEdge.API.Tests;

internal sealed class TestOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
{
    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    public TOptions CurrentValue { get; private set; } = currentValue;

    public TOptions Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<TOptions, string?> listener) => NoopDisposable.Instance;

    public void Set(TOptions nextValue) => CurrentValue = nextValue;
}
