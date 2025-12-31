using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Storage;
using System.Web;

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

    /// <summary>
    /// Upload an image to Azure Blob Storage.
    /// </summary>
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

    /// <summary>
    /// Retrieve an image from Azure Blob Storage by blob name.
    /// Example: GET /api/uploads/images/2024/01/15/abc123-photo.jpg
    /// </summary>
    [AllowAnonymous]
    [HttpGet("images/{*blobName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return Problem(title: "Validation error", detail: "Blob name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Decode URL-encoded blob name (Swagger encodes slashes as %2F)
        var decodedBlobName = HttpUtility.UrlDecode(blobName);

        var result = await _blobStorage.GetAsync(decodedBlobName, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Image not found." });
        }

        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Delete an image from Azure Blob Storage by blob name.
    /// Example: DELETE /api/uploads/images/2024/01/15/abc123-photo.jpg
    /// </summary>
    [AllowAnonymous]
    [HttpDelete("images/{*blobName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return Problem(title: "Validation error", detail: "Blob name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Decode URL-encoded blob name (Swagger encodes slashes as %2F)
        var decodedBlobName = HttpUtility.UrlDecode(blobName);

        var deleted = await _blobStorage.DeleteAsync(decodedBlobName, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { message = "Image not found or already deleted." });
        }

        return Ok(new { message = "Image deleted successfully." });
    }

    /// <summary>
    /// List all images in Azure Blob Storage with optional prefix filter.
    /// Example: GET /api/uploads/images?prefix=2024/01
    /// </summary>
    [AllowAnonymous]
    [HttpGet("images")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListImages([FromQuery] string? prefix, CancellationToken cancellationToken)
    {
        var blobs = await _blobStorage.ListAsync(prefix, cancellationToken);

        return Ok(new { images = blobs });
    }
}
