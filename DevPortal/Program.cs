using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if DEBUG
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
#endif

namespace DevPortal;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        
        var tileJsonPathHolder = new TileJsonPathHolder();
        
        builder.Services.AddSingleton(tileJsonPathHolder);
        builder.Services.AddSingleton<TileStore>();
        builder.Services.AddHostedService<TileReloadService>();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        var app = builder.Build();
        Icon.Initialize(app.Environment);
        tileJsonPathHolder.Path = GetTilesFilePath(args, app.Environment);

        var tileStore = app.Services.GetRequiredService<TileStore>();
        if (!await tileStore.TryLoadAsync(CancellationToken.None))
        {
            throw new Exception("Could not load tiles");
        }
        
#if DEBUG
        if (app.Environment.IsDevelopment())
        {
            var devFrontendPath = Path.GetFullPath(
                Path.Combine(app.Environment.ContentRootPath, "..", "frontend"));

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(devFrontendPath)
            });
        }
        else
#endif
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapStaticAssets();
        }

        app.MapGet("/api/tiles", (TileStore s) => s.Tiles);

        await app.RunAsync();
    }

    private static string GetTilesFilePath(string[] args, IWebHostEnvironment env)
    {
        if (args.Length > 0)
        {
            return Path.GetFullPath(args[0]);
        }

#if DEBUG
        if (env.IsDevelopment())
        {
            var examplePath = Path.Combine(env.ContentRootPath, "..", "tiles.example.json");
            if (File.Exists(examplePath)) return Path.GetFullPath(examplePath);
        }
#endif

        throw new Exception("No tiles file provided and no example file found.");
    }
}