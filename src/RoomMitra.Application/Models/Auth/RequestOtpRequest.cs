namespace RoomMitra.Application.Models.Auth;

public sealed class RequestOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}
