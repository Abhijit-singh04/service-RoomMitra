namespace RoomMitra.Application.Abstractions.Storage;

public interface IBlobStorage
{
    Task<string> UploadAsync(
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken
    );
}
