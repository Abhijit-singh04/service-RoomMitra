using RoomMitra.Application.Abstractions.Time;

namespace RoomMitra.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
