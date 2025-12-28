using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Storage;

namespace RoomMitra.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController : ControllerBase
{
    private const long MaxImageBytes = 5L * 1024 * 1024;

    private readonly IBlobStorage _blobStorage;

    public UploadsController(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public sealed class UploadImageRequest
    {
        public IFormFile File { get; init; } = default!;
    }

    [AllowAnonymous]
    [HttpPost("images")]
    [RequestSizeLimit(MaxImageBytes)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file.Length <= 0)
        {
            return Problem(title: "Validation error", detail: "File is empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > MaxImageBytes)
        {
            return Problem(title: "Validation error", detail: "Image size must be <= 5MB.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(title: "Validation error", detail: "Only image uploads are supported.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        var url = await _blobStorage.UploadAsync(stream, file.ContentType, file.FileName, cancellationToken);

        return Ok(new { url });
    }
}
