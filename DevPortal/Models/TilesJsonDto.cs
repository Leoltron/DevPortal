using System.Collections.Generic;

namespace DevPortal.Models;

public record TilesJsonDto(Tile[] Tiles, Dictionary<string, string>? TagsDescription = null, Dictionary<string, int>? CategoryPriorities = null);