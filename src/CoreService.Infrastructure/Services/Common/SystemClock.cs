using CoreService.Application.Abstractions.Common;

namespace CoreService.Infrastructure.Services.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
