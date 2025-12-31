namespace RoomMitra.Application.Models.Auth;

public sealed class RequestOtpResponse
{
    public RequestOtpResponse(string requestId)
    {
        RequestId = requestId;
    }

    public string RequestId { get; }
}
