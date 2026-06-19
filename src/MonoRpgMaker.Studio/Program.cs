using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;

namespace MonoRpgMaker.Studio;

/// <summary>The Avalonia desktop entry point for the map-paint studio (a thin host).</summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    /// <summary>Process entry point.</summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Configure the Avalonia application (also used by the XAML previewer).</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
