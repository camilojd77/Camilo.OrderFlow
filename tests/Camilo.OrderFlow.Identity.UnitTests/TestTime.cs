namespace Camilo.OrderFlow.Identity.UnitTests;

/// <summary>Fixed clock for deterministic tests (docs/adr/0025-timeprovider-en-todo-el-codigo.md).</summary>
internal static class TestTime
{
    public static readonly DateTime UtcNow = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    public static TimeProvider Provider { get; } = new FixedTimeProvider(new DateTimeOffset(UtcNow));
}

/// <summary>A <see cref="TimeProvider"/> that always returns the same instant.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
