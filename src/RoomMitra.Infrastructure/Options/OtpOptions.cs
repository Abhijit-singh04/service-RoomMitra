namespace RoomMitra.Infrastructure.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; init; } = 6;
    public int ExpiryMinutes { get; init; } = 5;
    public int MaxAttempts { get; init; } = 5;
    public int ResendCooldownSeconds { get; init; } = 60;
}
