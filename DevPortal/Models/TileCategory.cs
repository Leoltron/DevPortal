namespace DevPortal.Models;

public record TileCategory(string Name, Tile[] Tiles, int? Priority = null);
