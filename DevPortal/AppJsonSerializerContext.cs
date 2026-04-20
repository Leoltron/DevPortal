using System.Text.Json.Serialization;
using DevPortal.Models;

namespace DevPortal;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TilesDto))]
[JsonSerializable(typeof(TilesJsonDto))]
[JsonSerializable(typeof(TileCategory))]
[JsonSerializable(typeof(TileCategory[]))]
[JsonSerializable(typeof(Tile))]
[JsonSerializable(typeof(Tile[]))]
[JsonSerializable(typeof(Link))]
[JsonSerializable(typeof(Link[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;