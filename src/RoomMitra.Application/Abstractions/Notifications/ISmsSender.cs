namespace RoomMitra.Application.Abstractions.Notifications;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken);
}
