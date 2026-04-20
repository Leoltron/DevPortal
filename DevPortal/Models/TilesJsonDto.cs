using System.Collections.Generic;

namespace DevPortal.Models;

public record TilesJsonDto(TileCategory[] Categories, Dictionary<string, string>? TagsDescription = null);