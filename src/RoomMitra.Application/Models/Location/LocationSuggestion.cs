namespace RoomMitra.Application.Models.Location;

/// <summary>
/// Autocomplete suggestion returned to frontend.
/// Only contains what's needed - no extra data.
/// </summary>
public sealed record LocationSuggestion(
    string Label,
    double Lat,
    double Lon
);
