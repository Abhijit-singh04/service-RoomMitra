namespace RoomMitra.Infrastructure.Options;

/// <summary>
/// Configuration options for Firebase Admin SDK.
/// </summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// The Firebase project ID.
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Path to the Firebase service account JSON file.
    /// If not provided, the SDK will use default credentials.
    /// </summary>
    public string? ServiceAccountPath { get; init; }

    /// <summary>
    /// Whether to simulate/mock Firebase token verification.
    /// Set to true for development/testing without actual Firebase setup.
    /// </summary>
    public bool SimulateVerification { get; init; } = false;
}
