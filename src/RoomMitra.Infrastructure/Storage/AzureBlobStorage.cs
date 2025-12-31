using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Storage;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Storage;

internal sealed class AzureBlobStorage : IBlobStorage
{
    private readonly AzureBlobOptions _options;
    private readonly Lazy<BlobContainerClient> _containerClient;

    public AzureBlobStorage(IOptions<AzureBlobOptions> options)
    {
        _options = options.Value;
        _containerClient = new Lazy<BlobContainerClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new InvalidOperationException("Azure Blob connection string is not configured.");
            }

            var serviceClient = new BlobServiceClient(_options.ConnectionString);
            return serviceClient.GetBlobContainerClient(_options.ContainerName);
        });
    }

    private BlobContainerClient Container => _containerClient.Value;

    public async Task<string> UploadAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken)
    {
        // Use None for private containers (public access is disabled on storage account)
        await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "upload" : Path.GetFileName(fileName);
        var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        var blobClient = Container.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken
        );

        // Return just the blob name (path) - images will be served through the API endpoint
        return blobName;
    }

    public async Task<BlobFileResult?> GetAsync(string blobName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = Container.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var properties = response.Value.Details;

            return new BlobFileResult(
                response.Value.Content,
                properties.ContentType ?? "application/octet-stream",
                Path.GetFileName(blobName)
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = Container.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListAsync(string? prefix, CancellationToken cancellationToken)
    {
        var blobs = new List<string>();

        await foreach (var blobItem in Container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            blobs.Add(blobItem.Name);
        }

        return blobs;
    }
}
