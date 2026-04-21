using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevPortal;

public static partial class Icon
{
    private static readonly ILogger Logger = DevPortal.Logger.Create(typeof(Icon));
    private static readonly Dictionary<string, string> IconMap = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(IWebHostEnvironment env)
    {
        var iconDir = env.IsDevelopment()
            ? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "frontend", "public", "icons"))
            : Path.Combine(env.ContentRootPath, "wwwroot", "icons");

        if (!Directory.Exists(iconDir))
        {
            LogDirNotFound(Logger, iconDir);
            return;
        }

        IconMap.Clear();
        foreach (var file in Directory.GetFiles(iconDir, "*.svg"))
        {
            IconMap.Add(Path.GetFileNameWithoutExtension(file), Path.GetFileName(file));
        }

        LogIconsLoaded(Logger, iconDir, string.Join(", ", IconMap.Keys));
    }

    public static string Resolve(string? icon)
    {
        if (string.IsNullOrEmpty(icon)) return "";

        return IconMap.TryGetValue(icon, out var path) ? $"./icons/{path}" : icon;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Icon directory not found: {IconDir}")]
    private static partial void LogDirNotFound(ILogger logger, string iconDir);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded icons from {IconDir}: {IconsString}")]
    private static partial void LogIconsLoaded(ILogger logger, string iconDir, string iconsString);
}