using RoomMitra.Application.Abstractions.Notifications;

namespace RoomMitra.Infrastructure.Notifications;

internal sealed class ConsoleSmsSender : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[SMS] To: {phoneNumber} | Message: {message}");
        return Task.CompletedTask;
    }
}
