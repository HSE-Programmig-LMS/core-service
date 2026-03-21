using CoreService.Application.Abstractions.Common;

namespace CoreService.Tests.Unit.TestDoubles;

/// <summary>
/// Управляемые часы для unit-тестов. Позволяют фиксировать время и "прокручивать" его вперёд.
/// </summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset initialUtcNow)
    {
        UtcNow = initialUtcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}