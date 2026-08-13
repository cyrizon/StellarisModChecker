using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace StellarisModChecker.Services;

public class UpdateService
{
    private readonly UpdateManager _updateManager;

    public UpdateService()
    {
        var source = new GithubSource("https://github.com/cyrizon/StellarisModChecker", null, false);
        _updateManager = new UpdateManager(source);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            Log.Information("Vérification des mises à jour de l'application via Velopack...");
            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                Log.Information("Mise à jour disponible : version {TargetVersion}", updateInfo.TargetFullRelease.Version);
            }
            else
            {
                Log.Information("L'application est à jour (version actuelle : {CurrentVersion})", CurrentVersion);
            }

            return updateInfo;
        }
        catch (Velopack.Exceptions.NotInstalledException)
        {
            Log.Debug("Vérification Velopack ignorée (application non installée via le packageur).");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la vérification des mises à jour applicatives");
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallAsync(UpdateInfo updateInfo)
    {
        try
        {
            Log.Information("Téléchargement de la mise à jour version {Version}...", updateInfo.TargetFullRelease.Version);
            await _updateManager.DownloadUpdatesAsync(updateInfo);

            Log.Information("Mise à jour téléchargée. Redémarrage de l'application...");
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du téléchargement ou de l'application de la mise à jour Velopack");
            return false;
        }
    }

    public string CurrentVersion => _updateManager.CurrentVersion?.ToString() ?? "0.0.0";
}