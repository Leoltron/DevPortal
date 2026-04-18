using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DevPortal;

public class TileReloadService(TileStore store, TileJsonPathHolder pathHolder) : BackgroundService
{
    private FileSystemWatcher? _watcher;

    private readonly Channel<bool> _reloadSignal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropWrite
    });

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(pathHolder.Path) ?? "/";
        _watcher = new FileSystemWatcher(dir, "*.json")
        {
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.Size
                           | NotifyFilters.CreationTime
                           | NotifyFilters.FileName
        };

        _watcher.Changed += OnTileJsonFileUpdate;
        _watcher.Created += OnTileJsonFileUpdate;
        _watcher.Renamed += OnTileJsonFileUpdate;
        _watcher.EnableRaisingEvents = true;
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        var timerTask = Task.Run(() => RunTimerAsync(timer, token), token);

        while (!token.IsCancellationRequested)
        {
            await _reloadSignal.Reader.ReadAsync(token);
            await Task.Delay(100, token);
            while (_reloadSignal.Reader.TryRead(out _))
            {
            }

            await store.TryLoadAsync(token);
        }

        await timerTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private void OnTileJsonFileUpdate(object? _, FileSystemEventArgs __)
    {
        SignalReload();
    }

    private void SignalReload()
    {
        _reloadSignal.Writer.TryWrite(true);
    }

    private async Task RunTimerAsync(PeriodicTimer timer, CancellationToken token)
    {
        while (await timer.WaitForNextTickAsync(token))
        {
            SignalReload();
        }
    }
}