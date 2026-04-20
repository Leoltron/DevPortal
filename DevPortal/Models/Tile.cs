namespace DevPortal.Models;

public record Tile(
    Link Main,
    string Description,
    string[] Tags,
    string[] Aliases,
    Link[]? AdditionalLinks,
    int? Width = 3,
    int? Height = 3);