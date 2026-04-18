using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevPortal.Models;
using Microsoft.Extensions.Logging;

namespace DevPortal;

public partial class TileStore(TileJsonPathHolder pathHolder)
{
    private static readonly ILogger Logger = DevPortal.Logger.Create<TileStore>();
    public TilesDto Tiles => Volatile.Read(ref _tiles);

    private string? _lastReadJson;
    private TilesDto _tiles = new([], DateTime.Now);

    public async Task<bool> TryLoadAsync(CancellationToken token)
    {
        try
        {
            var dto = await ReadIfChangedAsync(token);
            if (dto == null)
            {
                return false;
            }

            var tiles = new TilesDto(dto.Tiles, DateTime.UtcNow, dto.TagsDescription);
            Volatile.Write(ref _tiles, tiles);
        }
        catch (Exception e)
        {
            Logger.LogError("An exception occurred while trying to read tiles JSON: {Exception}", e);
            _lastReadJson = null;
            return false;
        }

        return true;
    }

    private static bool WaitForFile(string path, CancellationToken token)
    {
        while (!IsFileReady(path))
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
        }

        return true;
    } 
    
    private static bool IsFileReady(string filename)
    {
        try
        {
            using var inputStream = File.OpenRead(filename);
            return inputStream.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<TilesJsonDto?> ReadIfChangedAsync(CancellationToken token)
    {
        var ctx = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        token = ctx.Token;


        var jsonPath = pathHolder.Path;
        if (!WaitForFile(jsonPath, token))
        {
            Logger.LogError("Tiles JSON file wait timed out.");
            return null;
        }
        
        LogReadingTilesFromPath(jsonPath);

        var tilesJson = await File.ReadAllTextAsync(jsonPath, token);
        if (string.IsNullOrWhiteSpace(tilesJson))
        {
            Logger.LogError("Tiles JSON file is empty");
            return null;
        }

        if (tilesJson == _lastReadJson)
        {
            Logger.LogDebug("Tiles JSON file did not change, skipping read");
            return null;
        }

        Logger.LogDebug("Tiles JSON file change detected");

        var dto = JsonSerializer.Deserialize(tilesJson, AppJsonSerializerContext.Default.TilesJsonDto);
        if (dto == null)
        {
            Logger.LogError("Tiles JSON deserialization returned null");
            return null;
        }

        foreach (var tile in dto.Tiles)
        {
            tile.Main.Icon = Icon.Resolve(tile.Main.Icon);

            if (tile.AdditionalLinks == null)
                continue;

            foreach (var link in tile.AdditionalLinks)
            {
                link.Icon = Icon.Resolve(link.Icon);
            }
        }

        LogReadTilesFromPath(jsonPath);
        _lastReadJson = tilesJson;
        return dto;
    }

    [LoggerMessage(LogLevel.Trace, "Reading tiles from {Path}")]
    partial void LogReadingTilesFromPath(string path);

    [LoggerMessage(LogLevel.Information, "Successfully read tiles from {Path}")]
    partial void LogReadTilesFromPath(string path);
}