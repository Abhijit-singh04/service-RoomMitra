namespace RoomMitra.Application.Models.Auth;

public sealed class VerifyOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string RequestId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
