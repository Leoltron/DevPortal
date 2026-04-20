using System;
using System.Collections.Generic;

namespace DevPortal.Models;

public record TilesDto(TileCategory[] Categories, DateTime LastUpdated, Dictionary<string, string>? TagsDescription = null);