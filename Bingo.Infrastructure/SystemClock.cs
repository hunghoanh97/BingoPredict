using Bingo.Application.Abstractions;

namespace Bingo.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
