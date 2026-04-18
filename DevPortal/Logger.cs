using System;
using Microsoft.Extensions.Logging;

namespace DevPortal;

public static class Logger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(config => config.AddConsole());

    public static ILogger Create<T>() => Factory.CreateLogger<T>();
    public static ILogger Create(Type type) => Factory.CreateLogger(type);
}