using RoomMitra.Domain.Common;

namespace RoomMitra.Domain.Entities;

public sealed class OtpRequest : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string OtpHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public bool Used { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public string Channel { get; set; } = "sms";
    public DateTimeOffset LastSentAt { get; set; }
    public string? RequestIp { get; set; }
}
