namespace RoomMitra.Application.Abstractions.Storage;

public interface IBlobStorage
{
    Task<string> UploadAsync(
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken
    );

    Task<BlobFileResult?> GetAsync(
        string blobName,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteAsync(
        string blobName,
        CancellationToken cancellationToken
    );

    Task<IEnumerable<string>> ListAsync(
        string? prefix,
        CancellationToken cancellationToken
    );
}

public sealed record BlobFileResult(
    Stream Content,
    string ContentType,
    string FileName
);
