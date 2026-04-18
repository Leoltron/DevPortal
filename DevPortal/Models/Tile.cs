namespace DevPortal.Models;

public record Tile(
    Link Main,
    string Description,
    string[] Tags,
    string[] Aliases,
    string? Category,
    Link[]? AdditionalLinks,
    int? Width = 3,
    int? Height = 3);