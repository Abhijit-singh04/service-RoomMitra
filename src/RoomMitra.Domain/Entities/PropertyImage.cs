using RoomMitra.Domain.Common;

namespace RoomMitra.Domain.Entities;

public sealed class PropertyImage : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key to Property
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    // Image Details
    public string ImageUrl { get; set; } = string.Empty; // Azure Blob Storage URL
    public bool IsCover { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}
