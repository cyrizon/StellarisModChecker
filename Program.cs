using Avalonia;
using System;
using System.IO;
using Serilog;
using Velopack;

namespace StellarisModChecker;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Définition du dossier de logs dans LocalApplicationData
        string logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StellarisModChecker",
            "logs"
        );

        // 2. Configuration de Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logFolder, "app-.log"),
                rollingInterval: RollingInterval.Day, // Un nouveau fichier par jour (ex: app-20260813.log)
                retainedFileCountLimit: 7,            // Garder seulement les 7 derniers jours
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        // 3. Gestion globale des exceptions non gérées (Crash handler)
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Crash critique non géré de l'application.");
            }
            Log.CloseAndFlush();
        };

        try
        {
            Log.Information("Démarrage de StellarisModChecker...");
            
            VelopackApp.Build().Run();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erreur lors de l'initialisation de l'application.");
        }
        finally
        {
            Log.Information("Fermeture de l'application.");
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
