using System;
using System.Collections.Generic;

namespace DevPortal.Models;

public record TilesDto(Tile[] Tiles, DateTime LastUpdated, Dictionary<string, string>? TagsDescription = null, Dictionary<string, int>? CategoryPriorities = null);