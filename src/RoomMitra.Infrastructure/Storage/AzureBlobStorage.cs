using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Storage;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Storage;

internal sealed class AzureBlobStorage : IBlobStorage
{
    private readonly AzureBlobOptions _options;

    public AzureBlobStorage(IOptions<AzureBlobOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob connection string is not configured.");
        }

        var serviceClient = new BlobServiceClient(_options.ConnectionString);
        var container = serviceClient.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "upload" : Path.GetFileName(fileName);
        var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken
        );

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.ContainerName}/{blobName}";
        }

        return blobClient.Uri.ToString();
    }
}
