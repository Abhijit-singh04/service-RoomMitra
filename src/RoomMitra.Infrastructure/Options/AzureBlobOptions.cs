namespace RoomMitra.Infrastructure.Options;

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "roommitra";
    public string PublicBaseUrl { get; init; } = string.Empty;
}
