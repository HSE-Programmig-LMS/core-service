namespace CoreService.Application.Abstractions.Common;

/// <summary>
/// Абстракция времени для тестируемости.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
